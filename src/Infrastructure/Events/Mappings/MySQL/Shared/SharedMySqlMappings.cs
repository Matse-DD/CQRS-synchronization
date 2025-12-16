using System;
using System.Collections.Generic;
using System.Text;

namespace Infrastructure.Events.Mappings.MySQL.Shared
{
    public static class SharedMySqlMappings
    {
        public static string MapWhereClause(IDictionary<string, string>? condition)
        {
            if (condition == null || !condition.Any()) return "True";

            return string.Join(" AND ", condition.Select(conditionPair =>
            {
                string key = conditionPair.Key;
                string value = conditionPair.Value.Trim();

                if (value.StartsWith(">=") || value.StartsWith("<=") || value.StartsWith('>') || value.StartsWith('<') || value.StartsWith('='))
                {
                    return $"{key}{value.DetermineMySqlValue()}";
                }

                return $"{key} = {value.DetermineMySqlValue()}";
            }));
        }
    }
}
