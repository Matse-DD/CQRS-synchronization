using Application.Contracts.Events.EventOptions;
using Application.Contracts.Events.Factory;
using Infrastructure.Events.Mappings.MySQL.Shared;
using System.Text.Json;

namespace Infrastructure.Events.Mappings.MySQL;

public class MySqlInsertEvent(IntermediateEvent intermediateEvent) : InsertEvent(intermediateEvent)
{
    public override object GetCommand()
    {
        string command = $"INSERT INTO {AggregateName} ({MapColumns(Properties.Keys)})\n" +
                         $"VALUES ({MapValues(Properties.Keys)})";

        Dictionary<string, string> parameterizedDict = BuildParamDict(Properties);

        return (command, parameterizedDict);
    }

    private static string MapColumns(IEnumerable<string> keys)
    {
        return string.Join(", ", keys);
    }

    private static string MapValues(IEnumerable<object> incomingParameters)
    {
        IEnumerable<string> parameters = incomingParameters.Select(parameter => $"@{parameter}");
        return string.Join(", ", parameters);
    }

    private static Dictionary<string, string> BuildParamDict(Dictionary<string, object> properties)
    {
        Dictionary<string, string> parameters = new Dictionary<string, string>();

        foreach(KeyValuePair<string, object> keyValuePair in properties)
        {
            parameters.Add($"@{keyValuePair.Key}", ConvertValue(keyValuePair.Value));
        }

        return parameters;
    }

    private static string ConvertValue(object incomingValue)
    {
        if (incomingValue is not JsonElement value) return "NULL";
        return value.ToString().DetermineMySqlValue();
    }
}
