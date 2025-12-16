using Application.Contracts.Events.EventOptions;
using Application.Contracts.Events.Factory;
using Infrastructure.Events.Mappings.MySQL.Shared;

namespace Infrastructure.Events.Mappings.MySQL;

public class MySqlDeleteEvent(IntermediateEvent intermediateEvent) : DeleteEvent(intermediateEvent)
{
    public override string GetCommand()
    {
        return $"DELETE FROM {AggregateName} WHERE {SharedMySqlMappings.MapWhereClause(Condition)}";
    }
}
