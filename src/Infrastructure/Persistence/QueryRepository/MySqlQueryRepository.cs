using Application.Contracts.Persistence;
using MySql.Data.MySqlClient;
using System.Data.Common;

namespace Infrastructure.Persistence.QueryRepository;

public class MySqlQueryRepository : IQueryRepository
{
    private readonly string _connectionString;

    public MySqlQueryRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async void Execute(string command, Guid eventId)
    {
        using MySqlConnection connection = new MySqlConnection(_connectionString);
        
        await connection.OpenAsync();

        string commandLastEventId = $@"UPDATE last_info SET last_event_id = ""{eventId}""";
        Console.WriteLine(commandLastEventId);

        //MySqlCommand cmdLastEventId = new MySqlCommand(commandLastEventId, _connection);
        //MySqlCommand cmdDataUpdate = new MySqlCommand(command, _connection);

        using MySqlTransaction transaction = connection.BeginTransaction();

        try
        {
            // 2. Initialize Commands and associate them with the Connection and Transaction
            using MySqlCommand cmdLastEventId = new MySqlCommand(commandLastEventId, connection, transaction);
            using MySqlCommand cmdDataUpdate = new MySqlCommand(command, connection, transaction);

            Console.WriteLine("begin update");
            
            int amountChangedLasteEventId = await cmdLastEventId.ExecuteNonQueryAsync();
            int amountChangedUpdate = await cmdDataUpdate.ExecuteNonQueryAsync();

            transaction.Commit();
        }
        catch (DbException ex)
        {
            Console.WriteLine("something went back rolling back");
            Console.WriteLine(ex.ToString());
            transaction.Rollback();

            throw new Exception(ex.Message);
        }
    }

    public async Task<Guid> GetLastSuccessfulEventId()
    {
        string queryLastEventId = $"SELECT last_event_id FROM last_info";

        using MySqlConnection connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        MySqlCommand cmdGetLastEventId = new MySqlCommand(queryLastEventId, connection);
        using DbDataReader result = await cmdGetLastEventId.ExecuteReaderAsync();

        if (!await result.ReadAsync())
        {
            Console.WriteLine("result is empty");
            return Guid.Empty;
        }

        int columnLastEventId = result.GetOrdinal("last_event_id");

        if (result.IsDBNull(columnLastEventId))
        {
            return Guid.Empty;
        }
        
        return result.GetGuid(columnLastEventId);
    }
}
