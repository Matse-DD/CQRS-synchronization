using Application.Contracts.Events.EventOptions;
using Application.Contracts.Events.Factory;
using Infrastructure.Events.Mappings.Shared;
using Infrastructure.Persistence;
using System.Text.Json;

namespace Infrastructure.Events.Mappings.MySQL;

public class MySqlInsertEvent(IntermediateEvent intermediateEvent) : InsertEvent(intermediateEvent)
{
    public override CommandInfo GetCommandInfo()
    {
        string command = $"INSERT INTO {AggregateName.Sanitize()} ({MapColumns(Properties.Keys)})\n" +
                         $"VALUES ({MapValues(Properties.Keys)})";

        return new CommandInfo(command, BuildParamDict(Properties));
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

    private static Dictionary<string, object> BuildParamDict(Dictionary<string, object> properties)
    {
        Dictionary<string, object> parameters = new Dictionary<string, object>();

        foreach (KeyValuePair<string, object> keyValuePair in properties)
        {
            parameters.Add($"@{keyValuePair.Key}", ConvertValue(keyValuePair.Value));
        }

        return parameters;
    }

    private static object ConvertValue(object incomingValue)
    {
        if (incomingValue is not JsonElement value) return "NULL";
        return value.ToString().ExtractValue();
    }
}
