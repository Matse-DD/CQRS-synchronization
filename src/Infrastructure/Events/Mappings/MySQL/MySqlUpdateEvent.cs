using Application.Contracts.Events.EventOptions;
using Application.Contracts.Events.Factory;
using Application.Contracts.Persistence;
using Infrastructure.Events.Mappings.MySQL.Shared;
using Infrastructure.Events.Mappings.Shared;
using static Mysqlx.Expect.Open.Types.Condition.Types;

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
        return string.Join(", ", change.Select(changePair => MapChangePart(prefix, changePair)));
    }

    private static string MapChangePart(string prefix, KeyValuePair<string, string> changePart)
    {
        string onProperty = changePart.Key;
        string sign = changePart.Value.ExtractSign();

        if (sign == "") return ImplicitEqualsChange(prefix, onProperty);

        return ExplicitChange(prefix, onProperty, sign);
    }

    private static string ExplicitChange(string prefix, string onProperty, string sign)
    {
        return $"{onProperty} = {onProperty} {sign} @{prefix}_{onProperty}";
    }

    private static string ImplicitEqualsChange(string prefix, string onProperty)
    {
        return $"{onProperty} = @{prefix}_{onProperty}";
    }
}
