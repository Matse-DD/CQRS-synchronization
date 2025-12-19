using Google.Protobuf.WellKnownTypes;
using static Mysqlx.Expect.Open.Types.Condition.Types;

namespace Infrastructure.Events.Mappings.MySQL.Shared;

public static class SharedMySqlMappings
{
    public static string MapWhereClause(IDictionary<string, string>? condition)
    {
        if (!DoesConditionExist(condition)) return "True";

        return string.Join(" AND ", condition!.Select(conditionPair =>
        {
            string key = conditionPair.Key;
            string value = conditionPair.Value.Trim();

            if (HasConditionSign(value))
            {
                return $"{key}{value.DetermineMySqlValue()}";
            }

            return $"{key} = {value.DetermineMySqlValue()}";
        }));
    }

    private static bool HasConditionSign(string value)
    {
        return value.StartsWith(">=") || value.StartsWith("<=") || value.StartsWith('>') || value.StartsWith('<') || value.StartsWith('=');
    }

    private static bool DoesConditionExist(IDictionary<string, string>? condition)
    {
        return condition == null || !condition.Any();
    }
}
