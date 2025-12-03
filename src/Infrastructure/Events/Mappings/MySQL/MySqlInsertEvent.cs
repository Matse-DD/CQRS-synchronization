using Application.Contracts.Events;
using System.Text.Json;

namespace Infrastructure.Events.Mappings.MySQL;

public class MySqlInsertEvent : Event
{
    public MySqlInsertEvent(string incomingEvent) : base(incomingEvent)
    {
        Dictionary<string, object> propertiesEvent = JsonSerializer.Deserialize<Dictionary<string, object>>(incomingEvent);
        Dictionary<string, object> insertPayload = JsonSerializer.Deserialize<Dictionary<string, object>>(propertiesEvent["payload"].ToString());
        PayLoad = new InsertPayload(insertPayload);
    }

    public override string GetCommand()
    {
        return
            $"INSERT INTO {AggregateName} ({PayLoad.GetValuePairs().Keys.ToList().Aggregate("",
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
            $"VALUES ({GetValuesReduced(PayLoad.GetValuePairs().Values)})";
    }


    private static string GetValuesReduced(IEnumerable<object> incomingValues)
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
