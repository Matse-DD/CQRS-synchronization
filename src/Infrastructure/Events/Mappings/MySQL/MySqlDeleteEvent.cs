using Application.Contracts.Events.EventOptions;
using Application.Contracts.Events.Factory;
using Google.Protobuf.WellKnownTypes;
using Infrastructure.Events.Mappings.MySQL.Shared;
using Infrastructure.Persistence;
using MySql.Data.MySqlClient;

namespace Infrastructure.Events.Mappings.MySQL;

public class MySqlDeleteEvent(IntermediateEvent intermediateEvent) : DeleteEvent(intermediateEvent)
{
    public override PersistenceCommandInfo GetCommandInfo()
    {
        (string ParameterizedWhere, Dictionary<string, string> ParameterDict) parameterizedWhere = MapParameterizedWhere(Condition); //TODO vragen of iedereen dit leesbaar vind

        string command = $"DELETE FROM {AggregateName.Sanitize()} WHERE {parameterizedWhere.ParameterizedWhere}";

        return new PersistenceCommandInfo(command, parameterizedWhere.ParameterDict);
    }

    private static (string, Dictionary<string, string>) MapParameterizedWhere(Dictionary<string, string> condition)
    {
        ICollection<string> parameterizedWhere = new List<string>();
        Dictionary<string, string> parametersWithValue = new Dictionary<string, string>();

        foreach (KeyValuePair<string, string> keyValuePair in condition)
        {
            string onProperty = keyValuePair.Key;
            string sign = keyValuePair.Value.ExtractSign();
            string parameterizedValue = $"@{onProperty}";

            if (HasConditionSign(sign))
            {
                parameterizedWhere.Add($"{onProperty} {sign} {parameterizedValue}");
            }
            else
            {
                parameterizedWhere.Add($"{onProperty} = {parameterizedValue}");
            }

            parametersWithValue.Add(parameterizedValue, keyValuePair.Value.ExtractValue());
        }

        return (string.Join(" AND ", parameterizedWhere), parametersWithValue);
    }

    private static bool HasConditionSign(string sign)
    {
        return sign.Equals(">=") || sign.Equals("<=") || sign.Equals("<>") || sign.Equals('>') || sign.Equals('<') || sign.Equals('=');
    }
}
