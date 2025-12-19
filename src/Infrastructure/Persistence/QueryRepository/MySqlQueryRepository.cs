using Application.Contracts.Persistence;
using Infrastructure.Replay;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using System.Data.Common;

namespace Infrastructure.Persistence.QueryRepository;

public class MySqlQueryRepository(string connectionString, ILogger<MySqlQueryRepository> logger) : IQueryRepository
{
    public async Task Execute(string command, Guid eventId)
    {
        using MySqlConnection connection = await OpenMySqlConnection();
        using MySqlTransaction transaction = await connection.BeginTransactionAsync();

        try
        {
            await ExecuteUpdateCommand(command, connection, transaction);
            await UpdateLastEventId(eventId, connection, transaction);

            await transaction.CommitAsync();
        }
        catch (DbException ex)
        {
            await RollbackTransaction(transaction, ex);
            throw;
        }
    }

    public async Task Clear()
    {
        using MySqlConnection connection = await OpenMySqlConnection();
        using MySqlTransaction transaction = await connection.BeginTransactionAsync();
        
        try
        {
            ICollection<string> tables = await GetTablesFromDatabase(connection);

            await DeleteTablesFromDatabase(connection, transaction, tables);

            await transaction.CommitAsync();
            logger.LogDebug("Cleared repository tables");
        }
        catch (DbException ex)
        {
            await RollbackTransaction(transaction, ex);
            throw;
        }
    }

    public async Task<Guid> GetLastSuccessfulEventId()
    {
        using MySqlConnection connection = await OpenMySqlConnection();

        Guid resultGuid = Guid.Empty;

        const string queryLastEventId = "SELECT last_event_id FROM last_info";

        using MySqlCommand cmdGetLastEventId = new MySqlCommand(queryLastEventId, connection);
        using DbDataReader result = await cmdGetLastEventId.ExecuteReaderAsync();
        
        if (await result.ReadAsync())
        {
            resultGuid = GetLastEventIdFromResult(result);
        }

        return resultGuid;
    }

    public async static Task CreateBasicStructureQueryDatabase(string queryDatabaseName, string connectionString, ILogger<MySqlQueryRepository> logger)
    {
        string commandCreateDatabase = $"CREATE DATABASE IF NOT EXISTS {queryDatabaseName};";
        string commandCreateTable = $"CREATE TABLE IF NOT EXISTS last_info (id INT, last_event_id VARCHAR(36), PRIMARY KEY (id));";
        string commandInsertTable = $"REPLACE INTO last_info VALUES(1, '{Guid.Empty}');";

        using MySqlConnection connection = new MySqlConnection(connectionString);
        await connection.OpenAsync();

        using MySqlCommand createDatabase = new MySqlCommand(commandCreateDatabase, connection);
        await createDatabase.ExecuteNonQueryAsync();

        using MySqlCommand createTable = new MySqlCommand(commandCreateTable, connection);
        await createTable.ExecuteNonQueryAsync();

        using MySqlCommand insertTable = new MySqlCommand(commandInsertTable, connection);
        await insertTable.ExecuteNonQueryAsync();

        logger.LogInformation("Created {queryDatabaseName} database with empty.", queryDatabaseName);
        logger.LogInformation("Initialized 'last_info' table with empty GUID.");
    }

    private async Task<MySqlConnection> OpenMySqlConnection()
    {
        MySqlConnection connection = new MySqlConnection(connectionString);
        await connection.OpenAsync();

        return connection;
    }

    private async Task UpdateLastEventId(Guid eventId, MySqlConnection connection, MySqlTransaction transaction)
    {
        string commandLastEventId = $@"REPLACE INTO last_info VALUES(1, '{eventId}')";

        logger.LogDebug("Updating LastEventId: {CommandLastEventId}", commandLastEventId);

        MySqlCommand cmdLastEventId = new MySqlCommand(commandLastEventId, connection, transaction);
        await cmdLastEventId.ExecuteNonQueryAsync();
    }

    private async Task ExecuteUpdateCommand(string command, MySqlConnection connection, MySqlTransaction transaction)
    {
        logger.LogInformation("Executing Update: {Command}", command);

        MySqlCommand cmdDataUpdate = new MySqlCommand(command, connection, transaction);
        await cmdDataUpdate.ExecuteNonQueryAsync();
    }

    private async Task<ICollection<string>> GetTablesFromDatabase(MySqlConnection connection)
    {
        string tableQuery = "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = DATABASE()";

        using MySqlCommand cmd = new MySqlCommand(tableQuery, connection);
        using DbDataReader reader = await cmd.ExecuteReaderAsync();

        ICollection<string> tables = [];

        while (await reader.ReadAsync())
        {
            tables.Add(reader.GetString(0));
        }

        return tables;
    }

    private static async Task DeleteTablesFromDatabase(MySqlConnection connection, MySqlTransaction transaction, ICollection<string> tables)
    {
        foreach (string tableName in tables)
        {
            string delete = $"DELETE FROM {tableName}";
            using MySqlCommand cmdDelete = new MySqlCommand(delete, connection, transaction);

            await cmdDelete.ExecuteNonQueryAsync();
        }
    }

    private async Task RollbackTransaction(MySqlTransaction transaction, DbException ex)
    {
        logger.LogError(ex, "Transaction failed. Rolling back.");
        await transaction.RollbackAsync();
    }

    private static Guid GetLastEventIdFromResult(DbDataReader result)
    {
        int columnLastEventId = result.GetOrdinal("last_event_id");

        if (result.IsDBNull(columnLastEventId))
        {
            return Guid.Empty;
        }

        return result.GetGuid(columnLastEventId);
    }
}