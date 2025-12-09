using Application.Contracts.Persistence;
using MySql.Data;
using MySql.Data.MySqlClient;
using MySqlX.XDevAPI;

namespace Infrastructure.Persistence;

public class MySqlRepository : IQueryRepository
{
    private readonly MySqlConnection _connection;

    public MySqlRepository(string connectionString)
    {
        _connection = new MySqlConnection(connectionString);
    }

    public void Execute(string command, Guid eventId)
    {
        string commandLastEventId = $"UPDATE last_event_id SET last_event_id = {eventId} FROM last_info";

        MySqlCommand cmdLastEventId = new MySqlCommand(commandLastEventId, _connection);
        MySqlCommand cmdDataUpdate = new MySqlCommand(command, _connection);

        MySqlTransaction transaction = _connection.BeginTransaction();

        cmdLastEventId.ExecuteReader();
        cmdDataUpdate.ExecuteReader();

        transaction.Commit();
    }

    public Guid GetLastSuccessfulEventId()
    {
        throw new NotImplementedException();
    }
}
