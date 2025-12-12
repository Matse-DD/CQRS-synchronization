using Application.Contracts.Persistence;
using MySql.Data.MySqlClient;
using Mysqlx.Crud;
using System.Data.Common;
using System.Threading.Tasks;

namespace Infrastructure.Persistence.QueryRepository;

public class MySqlQueryRepository(string connectionString) : IQueryRepository
{
    public async Task Execute(string command, Guid eventId)
    {
        using MySqlConnection connection = new MySqlConnection(connectionString);
        await connection.OpenAsync();

        string commandLastEventId = $@"UPDATE last_info SET last_event_id = ""{eventId}""";

        // TODO change this to logger
        Console.WriteLine(command);
        Console.WriteLine(commandLastEventId);
        Console.WriteLine();

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
            Console.WriteLine("something went wrong rolling back");
            Console.WriteLine(ex.ToString());
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task Clear()
    {
        using MySqlConnection connection = new MySqlConnection(connectionString);
        await connection.OpenAsync();

        using MySqlTransaction transaction = await connection.BeginTransactionAsync();
        try
        {
            string tableQuery = "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = DATABASE()";
            using MySqlCommand cmd = new MySqlCommand(tableQuery, connection);
            using DbDataReader reader = await cmd.ExecuteReaderAsync();

            ICollection<string> tables = [];

            while (await reader.ReadAsync())
            {
                tables.Add(reader.GetString(0));
            }

            reader.Close();

            foreach (string tableName in tables)
            {
                string delete = $"DELETE FROM {tableName}";
                using MySqlCommand cmdDelete = new MySqlCommand(delete, connection, transaction);
                await cmdDelete.ExecuteNonQueryAsync();
            }

            await transaction.CommitAsync();
            Console.WriteLine("Cleared repository tables");
        }
        catch (DbException e)
        {
            Console.WriteLine($"Error during clear, rolling back: {e.Message}");
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<Guid> GetLastSuccessfulEventId()
    {
        using MySqlConnection connection = new MySqlConnection(connectionString);
        await connection.OpenAsync();

        Guid resultGuid = Guid.Empty;
        bool needsDefaultValue = false;

        const string queryLastEventId = "SELECT last_event_id FROM last_info";
        using MySqlCommand cmdGetLastEventId = new MySqlCommand(queryLastEventId, connection);
        using (DbDataReader result = await cmdGetLastEventId.ExecuteReaderAsync())
        {
            if (await result.ReadAsync())
            {
                int columnLastEventId = result.GetOrdinal("last_event_id");
                resultGuid = result.IsDBNull(columnLastEventId) ? Guid.Empty : result.GetGuid(columnLastEventId);
            }
            else
            {
                needsDefaultValue = true;
            }
        }

        if (needsDefaultValue) await PlaceEmptyGuidInLastEventId(connection);

        return resultGuid;
    }

    private async Task PlaceEmptyGuidInLastEventId(MySqlConnection connection)
    {
        string commandCreatePlace = $"INSERT INTO last_info VALUES(1, {Guid.Empty})";
        using MySqlCommand createDefaultValueForLastInfo = new MySqlCommand(commandCreatePlace, connection);
        await createDefaultValueForLastInfo.ExecuteNonQueryAsync();

        Console.WriteLine("Placed empty guid in last_info...");
    }
}