using Application.Contracts.Events.EventOptions;
using Application.Contracts.Events.Factory;
using Application.Contracts.Persistence;
using Infrastructure.Events.Mappings.MySQL.Shared;
using Infrastructure.Events.Mappings.Shared;

namespace Infrastructure.Events.Mappings.MySQL;

public class MySqlUpdateEvent(IntermediateEvent intermediateEvent) : UpdateEvent(intermediateEvent)
{
    public override CommandInfo GetCommandInfo()
    {
        string command = $"UPDATE {AggregateName.Sanitize()}\n" +
                         $"SET {MapSet(nameof(Change), Change)}\n" +
                         $"WHERE {SharedMySqlMappings.MapWhere(nameof(Condition), Condition)}";

        Dictionary<string, object> parametersCondition = SharedMySqlMappings.MapValuesToParameters(nameof(Condition), Condition);
        Dictionary<string, object> parametersChange = SharedMySqlMappings.MapValuesToParameters(nameof(Change), Change);

        Dictionary<string, object> combined = parametersCondition.Concat(parametersChange)
                                                                 .ToDictionary(k => k.Key, v => v.Value);
        return new CommandInfo(command, combined);
    }

    private static string MapSet(string prefix, IDictionary<string, string> change)
    {
        return string.Join(", ", change.Select(changePair => ParameterizedChange(prefix, changePair)));
    }

    private static string ParameterizedChange(string prefix, KeyValuePair<string, string> changePart)
    {
        return $"{changePart.Key} = {DetermineChange(prefix, changePart.Key, changePart.Value)}";
    }

    private static string DetermineChange(string prefix, string key, string change)
    {
        string sign = change.ExtractSign();

        return $"{key} {sign} @{prefix}_{key}";
    }
}
