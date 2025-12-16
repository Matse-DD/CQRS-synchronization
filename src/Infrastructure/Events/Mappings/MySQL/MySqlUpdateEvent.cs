using Application.Contracts.Events.EventOptions;
using Application.Contracts.Events.Factory;
using Infrastructure.Events.Mappings.MySQL.Shared;

namespace Infrastructure.Events.Mappings.MySQL;

public class MySqlUpdateEvent(IntermediateEvent intermediateEvent) : UpdateEvent(intermediateEvent)
{
    public override string GetCommand()
    {
        return $"UPDATE {AggregateName}\n" +
               $"SET {MapSetClause(Change)}\n" +
               $"WHERE {SharedMySqlMappings.MapWhereClause(Condition)}";
    }

    private static string MapSetClause(IDictionary<string, string> change)
    {
        return string.Join(", ", change.Select(changePair => $"{changePair.Key} = {changePair.Value}"));
    }
}
