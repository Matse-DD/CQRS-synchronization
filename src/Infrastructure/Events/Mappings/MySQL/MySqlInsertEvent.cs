using Application.Contracts.Events;
using System.Text.Json;

namespace Infrastructure.Events.Mappings.MySQL;

public class MySqlInsertEvent(IntermediateEvent intermediateEvent) : InsertEvent(intermediateEvent)
{
    public override string GetCommand()
    {
        return
            $"INSERT INTO {AggregateName} ({GetKeysReduced(Properties.Keys)})\n" +
            $"VALUES ({GetValuesReduced(Properties.Values)})";
    }

    private string GetKeysReduced(IEnumerable<string> keys)
    {
        return string.Join(", ", keys);
    }

    private string GetValuesReduced(IEnumerable<object> incomingValues)
    {
        IEnumerable<string> convertedValues = incomingValues.Select(value => ConvertValue(value));
        return string.Join(", ", convertedValues);
    }

    private string ConvertValue(object incomingValue)
    {
        Console.WriteLine(incomingValue.GetType().Name);
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
