using Application.Contracts.Events.EventOptions;
using Application.Contracts.Events.Factory;
using Infrastructure.Events.Mappings.MySQL.Shared;
using System.Text.Json;

namespace Infrastructure.Events.Mappings.MySQL;

public class MySqlInsertEvent(IntermediateEvent intermediateEvent) : InsertEvent(intermediateEvent)
{
    public override string GetCommand()
    {
        return $"INSERT INTO {AggregateName} ({MapColumns(Properties.Keys)})\n" +
               $"VALUES ({MapValues(Properties.Values)})";
    }

    private static string MapColumns(IEnumerable<string> keys)
    {
        return string.Join(", ", keys);
    }

    private static string MapValues(IEnumerable<object> incomingValues)
    {
        IEnumerable<string> convertedValues = incomingValues.Select(ConvertValue);
        return string.Join(", ", convertedValues);
    }

    private static string ConvertValue(object incomingValue)
    {
        if (incomingValue is not JsonElement value) return "NULL";
        return value.ToString().DetermineMySqlValue();
    }
}
