using Google.Protobuf.WellKnownTypes;
using static Mysqlx.Expect.Open.Types.Condition.Types;

namespace Infrastructure.Events.Mappings.MySQL.Shared;

public static class SharedMySqlMappings
{
    public static string MapWhereClause(IDictionary<string, string>? condition)
    {
        if (!DoesConditionExist(condition)) return "True";

        return string.Join(" AND ", condition!.Select(DetermineMySqlConditionCheck));
    }

    private static string DetermineMySqlConditionCheck(KeyValuePair<string, string> itemInCondition)
    {
        string key = itemInCondition.Key;
        string value = itemInCondition.Value.Trim();
        string sqlValue = value.DetermineMySqlValue();

        if (HasConditionSign(value))
        {
            return $"{key}{sqlValue}";
        }

        return $"{key} = {sqlValue}";
    }

    private static bool HasConditionSign(string value)
    {
        return value.StartsWith(">=") || value.StartsWith("<=") || value.StartsWith("<>") || value.StartsWith('>') || value.StartsWith('<') || value.StartsWith('=');
    }

    private static bool DoesConditionExist(IDictionary<string, string>? condition)
    {
        return condition == null || !condition.Any();
    }
}
