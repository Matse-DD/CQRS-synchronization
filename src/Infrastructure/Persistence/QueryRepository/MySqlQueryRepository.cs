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
    
    public async Task Execute(string command, Guid eventId)
    {
        using MySqlConnection connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        string commandLastEventId = $@"UPDATE last_info SET last_event_id = ""{eventId}""";
        
        using MySqlTransaction transaction = await connection.BeginTransactionAsync();

        try
        {
            using MySqlCommand cmdLastEventId = new MySqlCommand(commandLastEventId, connection, transaction);
            using MySqlCommand cmdDataUpdate = new MySqlCommand(command, connection, transaction);
            
            await cmdLastEventId.ExecuteNonQueryAsync();
            await cmdDataUpdate.ExecuteNonQueryAsync();

            await transaction.CommitAsync();
        }
        catch (DbException e)
        {
            Console.WriteLine(e.Message);
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<Guid> GetLastSuccessfulEventId()
    {
        string queryLastEventId = $"SELECT last_event_id FROM last_info";

        using MySqlConnection connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        MySqlCommand cmdGetLastEventId = new MySqlCommand(queryLastEventId, connection);
        using DbDataReader result = await cmdGetLastEventId.ExecuteReaderAsync();

        if (!await result.ReadAsync()) return Guid.Empty;

        int columnLastEventId = result.GetOrdinal("last_event_id");
        if (result.IsDBNull(columnLastEventId)) return Guid.Empty;
        
        return result.GetGuid(columnLastEventId);
    }
}