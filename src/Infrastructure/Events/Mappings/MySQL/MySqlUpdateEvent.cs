using Application.Contracts.Events;

namespace Infrastructure.Events.Mappings.MySQL;

/*
 * // UPDATE 
{
  ""event_id"": ""84c9d1a3-b0e7-4f6c-9a2f-1e5b8d2c6f0a"",
  ""occured_at"": ""2025-11-29T17:15:00Z"",
  ""aggregate_name"": ""Product"",
  ""status"": ""PENDING"",
  ""type_event"": ""UPDATE"",
  ""payload"": {
    ""condition"": {
        ""amount_sold"": "">5"",
        ""price"": "">10""
    },
    ""change"": {
        ""price"": ""price * 1.10"",
        ""amount_sold"":""amount_sold + 1""
    }
  }
}

// MYSQL UPDATE
UPDATE table_name
SET column1 = value1, column2 = value2, ...
WHERE condition; 
*/
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

            if (value.StartsWith(">=") || value.StartsWith("<=") || value.StartsWith(">") || value.StartsWith("<") || value.StartsWith("="))
            {
                return $"{key} {value}";
            }
            else
            {
                return $"{key} = {value}";
            }
        }));
    }
}
