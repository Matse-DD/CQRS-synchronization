using Application.Contracts.Persistence;
using MySql.Data.MySqlClient;
using System.Data.Common;

namespace Infrastructure.Persistence.QueryRepository;

public class MySqlQueryRepository : IQueryRepository
{
    private readonly MySqlConnection _connection;

    public MySqlQueryRepository(string connectionString)
    {
        _connection = new MySqlConnection(connectionString);
        _connection.Open();
    }

    public async void Execute(string command, Guid eventId)
    {
        string commandLastEventId = $"UPDATE last_info SET last_event_id = {eventId}";

        MySqlCommand cmdLastEventId = new MySqlCommand(commandLastEventId, _connection);
        MySqlCommand cmdDataUpdate = new MySqlCommand(command, _connection);

        MySqlTransaction transaction = _connection.BeginTransaction();

        try
        {
            await cmdLastEventId.ExecuteNonQueryAsync();
            await cmdDataUpdate.ExecuteNonQueryAsync();

            transaction.Commit();
        }
        catch (DbException)
        {
            transaction.Rollback();
        }
    }

    public async Task<Guid> GetLastSuccessfulEventId()
    {
        string queryLastEventId = $"SELECT last_event_id FROM last_info";

        MySqlCommand cmdGetLastEventId = new MySqlCommand(queryLastEventId, _connection);
        DbDataReader result = await cmdGetLastEventId.ExecuteReaderAsync();

        if (!await result.ReadAsync())
        {
            return Guid.Empty;
        }

        return result.GetGuid(result.GetOrdinal("last_event_id"));
    }
}
