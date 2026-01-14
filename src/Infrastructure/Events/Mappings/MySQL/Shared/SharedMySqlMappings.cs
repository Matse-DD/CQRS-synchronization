using Infrastructure.Events.Mappings.Shared;
namespace Infrastructure.Events.Mappings.MySQL.Shared;

public static class SharedMySqlMappings
{
    public static string MapWhere(string prefix, IDictionary<string, string>? condition)
    {
        if (!DoesConditionExist(condition)) return "True";

        return string.Join(" AND ", condition!.Select(conditionPart => MapConditionPart(prefix, conditionPart)));
    }

    private static string MapConditionPart(string prefix, KeyValuePair<string, string> conditionPart)
    {
        string onProperty = conditionPart.Key;
        string sign = conditionPart.Value.ExtractSign();
        string parameterizedValue = $"@{prefix}_{onProperty}";

        if (HasConditionSign(sign))
        {
            return $"{onProperty} {sign} {parameterizedValue}";
        }
        else
        {
            return $"{onProperty} = {parameterizedValue}";
        }
    }

    public static Dictionary<string, object> MapValuesToParameters(string prefix, Dictionary<string, string> incoming)
    {
        Dictionary<string, object> mappedValues = new Dictionary<string, object>();

        foreach (KeyValuePair<string, string> keyValuePair in incoming)
        {
            string onProperty = keyValuePair.Key;
            string parameterizedValue = $"@{prefix}_{onProperty}";

            object value = keyValuePair.Value.ExtractValue();
            mappedValues.Add(parameterizedValue, value);
        }

        return mappedValues;
    }

    private static bool HasConditionSign(string sign)
    {
        return sign != "";
    }

    private static bool DoesConditionExist(IDictionary<string, string>? condition)
    {
        return condition != null || condition!.Any();
    }
}
