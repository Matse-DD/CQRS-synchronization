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

    private static string MapValuesClause(IEnumerable<object> incomingValues)
    {
        IEnumerable<string> convertedValues = incomingValues.Select(ConvertValue);
        return string.Join(", ", convertedValues);

        static string ConvertValue(object incomingValue)
        {
            if (incomingValue is not JsonElement value) return "NULL";
            return value.ValueKind == JsonValueKind.String ? $"\"{value}\"" : value.ToString();
        }
    }
}
