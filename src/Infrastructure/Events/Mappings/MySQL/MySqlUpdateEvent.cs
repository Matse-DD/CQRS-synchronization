using Application.Contracts.Events.EventOptions;
using Application.Contracts.Events.Factory;

namespace Infrastructure.Events.Mappings.MySQL;

public class MySqlUpdateEvent(IntermediateEvent intermediateEvent) : UpdateEvent(intermediateEvent)
{
    public override string GetCommand()
    {
        return $"UPDATE {AggregateName}\n" +
               $"SET {MapSetClause(Change)}\n" +
               $"WHERE {MapWhereClause(Condition)}";
    }

    private string MapSetClause(IDictionary<string, string> change)
    {
        return string.Join(", ", change.Select(changePair =>
        {
            return $"{changePair.Key} = {changePair.Value}";
        }));
    }

    private string MapWhereClause(IDictionary<string, string> condition)
    {
        if (condition == null || !condition.Any()) return "True";

        return string.Join(" AND ", condition.Select(conditionPair =>
        {
            string key = conditionPair.Key;
            string value = conditionPair.Value.Trim();

            if (value.StartsWith(">=") || value.StartsWith("<=") || value.StartsWith('>') || value.StartsWith('<') || value.StartsWith('='))
            {
                return $"{key}{value}";
            }
            else
            {
                return $"{key} = {value}";
            }
        }));
    }
}
