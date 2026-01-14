using Application.Contracts.Events.EventOptions;
using Application.Contracts.Events.Factory;
using Google.Protobuf.WellKnownTypes;
using Infrastructure.Events.Mappings.MySQL.Shared;
using Infrastructure.Persistence;
using MySql.Data.MySqlClient;

namespace Infrastructure.Events.Mappings.MySQL;

public class MySqlDeleteEvent(IntermediateEvent intermediateEvent) : DeleteEvent(intermediateEvent)
{
    public override PersistenceCommandInfo GetCommandInfo()
    {
        string command = $"DELETE FROM {AggregateName.Sanitize()} WHERE {SharedMySqlMappings.MapWhere(nameof(Condition),Condition)}";

        Dictionary<string, object> parametersWithValue = SharedMySqlMappings.MapValuesToParameters(nameof(Condition),Condition);

        return new PersistenceCommandInfo(command, parametersWithValue);
    }
}
