using Application.Contracts.Events.EventOptions;
using Application.Contracts.Events.Factory;
using Infrastructure.Events.Mappings.MySQL.Shared;

namespace Infrastructure.Events.Mappings.MySQL;

public class MySqlUpdateEvent(IntermediateEvent intermediateEvent) : UpdateEvent(intermediateEvent)
{
    public override string GetCommand()
    {
        return $"UPDATE {AggregateName}\n" +
               $"SET {MapSet(Change)}\n" +
               $"WHERE {SharedMySqlMappings.MapWhere(Condition)}";
    }

    private static string MapSet(IDictionary<string, string> change)
    {
        return string.Join(", ", change.Select(changePair =>
            {
                return $"{changePair.Key} = {changePair.Value.DetermineMySqlValue()}";
            }
        ));
    }
}
