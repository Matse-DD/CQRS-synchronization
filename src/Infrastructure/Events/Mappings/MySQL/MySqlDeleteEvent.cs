using Application.Contracts.Events.EventOptions;
using Application.Contracts.Events.Factory;
using Infrastructure.Events.Mappings.MySQL.Shared;
using Infrastructure.Events.Mappings.Shared;
using Infrastructure.Persistence;

namespace Infrastructure.Events.Mappings.MySQL;

public class MySqlDeleteEvent(IntermediateEvent intermediateEvent) : DeleteEvent(intermediateEvent)
{
    public override CommandInfo GetCommandInfo()
    {
        string command = $"DELETE FROM {AggregateName.Sanitize()} WHERE {SharedMySqlMappings.MapWhere(nameof(Condition),Condition)}";

        Dictionary<string, object> parametersWithValue = SharedMySqlMappings.MapValuesToParameters(nameof(Condition),Condition);

        return new CommandInfo(command, parametersWithValue);
    }
}
