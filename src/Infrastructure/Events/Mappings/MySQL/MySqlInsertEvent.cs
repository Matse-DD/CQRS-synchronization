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
        return keys.ToList().Aggregate("", (currentAccumulation, nextKey) =>
        {
            if (string.IsNullOrEmpty(currentAccumulation))
            {
                return nextKey;
            }
            else
            {
                return currentAccumulation + ", " + nextKey;
            }
        });
    }

    private string GetValuesReduced(IEnumerable<object> incomingValues)
    {
        return incomingValues.Aggregate("", (currentAccumulation, nextValue) =>
        {
            string convertedValue = ConvertValue(nextValue);
            if (string.IsNullOrEmpty(currentAccumulation))
            {
                return convertedValue;
            }
            else
            {
                return currentAccumulation + ", " + convertedValue;
            }
        });
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
