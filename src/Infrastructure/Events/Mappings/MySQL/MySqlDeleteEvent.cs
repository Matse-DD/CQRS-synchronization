using Application.Contracts.Events.EventOptions;
using Application.Contracts.Events.Factory;
using Google.Protobuf.WellKnownTypes;
using Infrastructure.Events.Mappings.MySQL.Shared;
using MySql.Data.MySqlClient;

namespace Infrastructure.Events.Mappings.MySQL;

public class MySqlDeleteEvent(IntermediateEvent intermediateEvent) : DeleteEvent(intermediateEvent)
{
    public override object GetCommand()
    {
        (string, Dictionary<string, string>) parameterizedWhere = MapParameterizedWhere(Condition);

        string command = $"DELETE FROM {AggregateName.Sanitize()} WHERE {parameterizedWhere.Item1}";

        return (command, parameterizedWhere.Item2);
    }

    private static (string, Dictionary<string, string>) MapParameterizedWhere(Dictionary<string, string> condition)
    {
        ICollection<string> parameterizedWhereConditions = new List<string>();
        Dictionary<string, string> parametersWithValue = new Dictionary<string, string>();

        foreach(KeyValuePair<string, string> keyValuePair in condition)
        {
            string onProperty = keyValuePair.Key;
            string sign = keyValuePair.Value.ExtractSign();
            string parameterizedValue = $"@{onProperty}";

            if (HasConditionSign(sign))
            {
                parameterizedWhereConditions.Add($"{onProperty} {sign} {parameterizedValue}");
            }
            else
            {
                parameterizedWhereConditions.Add($"{onProperty} = {parameterizedValue}");
            }

            Console.WriteLine(keyValuePair.Value.ExtractValue());

            parametersWithValue.Add(parameterizedValue, keyValuePair.Value.ExtractValue());
        }

        return (string.Join(" AND ", parameterizedWhereConditions), parametersWithValue);
    }

    private static bool HasConditionSign(string sign)
    {
        return sign.Equals(">=") || sign.Equals("<=") || sign.Equals("<>") || sign.Equals('>') || sign.Equals('<') || sign.Equals('=');
    }
}
