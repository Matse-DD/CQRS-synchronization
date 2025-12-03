using Application.Contracts.Events;
using System.Text.Json;

namespace Infrastructure.Events.Mappings.MySQL;

public class MySqlInsertEvent(IntermediateEvent intermediateEvent) : InsertEvent(intermediateEvent)
{
    public override string GetCommand()
    {
        return
            $"INSERT INTO {AggregateName} ({Properties.Keys.ToList().Aggregate("",
            (currentAccumulation, nextKey) =>
            {
                if (string.IsNullOrEmpty(currentAccumulation))
                {
                    return nextKey;
                }
                else
                {
                    return currentAccumulation + ", " + nextKey;
                }
            }
             )})\n" +
            $"VALUES ({getValuesReduced(Properties.Values)})";
    }


    private string getValuesReduced(IEnumerable<object> incomingValues)
    {
        string result = "";

        foreach (JsonElement incomingValue in incomingValues)
        {
            if (incomingValue is JsonElement value)
            {
                if (value.ValueKind == JsonValueKind.String)
                {
                    result += $"\"{value}\"";
                }
                else
                {
                    result += incomingValue;
                }

                result += ", ";
            }
        }

        if (result.Length >= 2)
        {
            result = result.Substring(0, result.Length - 2);
        }

        return result;
    }
}
