using Application.Contracts.Persistence;
using MySql.Data.MySqlClient;
using System.Data.Common;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Persistence.QueryRepository;

public class MySqlQueryRepository(string connectionString, ILogger<MySqlQueryRepository> logger) : IQueryRepository
{
    public async Task Execute(string command, Guid eventId)
    {
        using MySqlConnection connection = new MySqlConnection(connectionString);
        await connection.OpenAsync();

        string commandLastEventId = $@"UPDATE last_info SET last_event_id = ""{eventId}""";
        logger.LogInformation(command + "\n" + commandLastEventId + "\n\n");

        using MySqlTransaction transaction = await connection.BeginTransactionAsync();

        try
        {
            using MySqlCommand cmdLastEventId = new MySqlCommand(commandLastEventId, connection, transaction);
            using MySqlCommand cmdDataUpdate = new MySqlCommand(command, connection, transaction);

            await cmdLastEventId.ExecuteNonQueryAsync();
            await cmdDataUpdate.ExecuteNonQueryAsync();

            await transaction.CommitAsync();
        }
        catch (DbException ex)
        {
            logger.LogCritical("Something went wrong, rolling back transaction. Exception: {Exception}", ex);
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<Guid> GetLastSuccessfulEventId()
    {
        const string queryLastEventId = "SELECT last_event_id FROM last_info";

        using MySqlConnection connection = new MySqlConnection(connectionString);
        await connection.OpenAsync();

        MySqlCommand cmdGetLastEventId = new MySqlCommand(queryLastEventId, connection);
        using DbDataReader result = await cmdGetLastEventId.ExecuteReaderAsync();

        if (!await result.ReadAsync())
        {
            logger.LogWarning("Result is empty.");
            return Guid.Empty;
        }

        int columnLastEventId = result.GetOrdinal("last_event_id");
        return result.IsDBNull(columnLastEventId) ? Guid.Empty : result.GetGuid(columnLastEventId);
    }
}