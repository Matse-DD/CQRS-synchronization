using Application.Contracts.Events.EventOptions;
using Application.Contracts.Events.Factory;
using System.Text.Json;

namespace Infrastructure.Events.Mappings.MySQL;

public class MySqlInsertEvent(IntermediateEvent intermediateEvent) : InsertEvent(intermediateEvent)
{
    public override string GetCommand()
    {
        return
            $"INSERT INTO {AggregateName} ({MapColumns(Properties.Keys)})\n" +
            $"VALUES ({MapValuesClause(Properties.Values)})";
    }

    private string MapColumns(IEnumerable<string> keys)
    {
        return string.Join(", ", keys);
    }

    private string MapValuesClause(IEnumerable<object> incomingValues)
    {
        IEnumerable<string> convertedValues = incomingValues.Select(value => ConvertValue(value));
        return string.Join(", ", convertedValues);
    }

    private string ConvertValue(object incomingValue)
    {
        if (incomingValue is JsonElement value)
        {
            if (value.ValueKind == JsonValueKind.String)
            {
                return $"\"{value}\"";
            }
            else
            {
                return value.ToString();
            }
        }

        return "NULL";
    }
}
