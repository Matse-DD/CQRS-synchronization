using Application.Contracts.Events.EventOptions;
using Application.Contracts.Events.Factory;
using Application.Contracts.Persistence;
using Infrastructure.Events.Mappings.MySQL.Shared;
using Infrastructure.Events.Mappings.Shared;
using System.Text.Json;
using static Mysqlx.Expect.Open.Types;

namespace Infrastructure.Events.Mappings.MySQL;

public class MySqlInsertEvent(IntermediateEvent intermediateEvent) : InsertEvent(intermediateEvent)
{
    public override CommandInfo GetCommandInfo()
    {
        string command = $"INSERT INTO {AggregateName.Sanitize()} ({MapColumns(Properties.Keys)})\n" +
                         $"VALUES ({MapValues(Properties.Keys)})";

        Dictionary<string, object> parametersWithValue = MapValuesToParameters(Properties);

        return new CommandInfo(command, parametersWithValue);
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

    private static Dictionary<string, object> MapValuesToParameters(Dictionary<string, object> properties)
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
