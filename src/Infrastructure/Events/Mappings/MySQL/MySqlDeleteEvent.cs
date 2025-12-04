using Application.Contracts.Events;

namespace Infrastructure.Events.Mappings.MySQL;

public class MySqlDeleteEvent(IntermediateEvent intermediateEvent) : DeleteEvent(intermediateEvent)
{
    public override string GetCommand()
    {
        return
            $"DELETE FROM {AggregateName} WHERE {MapWhereClause(Condition)}";
    }
    private string MapWhereClause(IDictionary<string, string> condition)
    {
        if (condition == null || !condition.Any()) return "True";

        return string.Join(" AND ", condition.Select(conditionPair =>
        {
            string key = conditionPair.Key;
            string value = conditionPair.Value.Trim();

            if (value.StartsWith(">=") || value.StartsWith("<=") || value.StartsWith(">") || value.StartsWith("<") || value.StartsWith("="))
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
