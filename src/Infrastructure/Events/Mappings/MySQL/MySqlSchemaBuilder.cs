using Application.Contracts.Events;
using Application.Contracts.Events.EventOptions;
using Application.Contracts.Persistence;
using System.Text.Json;

namespace Infrastructure.Events.Mappings.MySQL;

public class MySqlSchemaBuilder : ISchemaBuilder
{
    private readonly HashSet<string> alreadyCreatedTables = new HashSet<string>();

    public async Task Map(IQueryRepository mySqlQueryRepository, InsertEvent insertEvent)
    {
        string aggregateName = insertEvent.AggregateName;
        if (alreadyCreatedTables.Contains(aggregateName)) return;

        string command = $"CREATE TABLE IF NOT EXISTS {aggregateName} ({DetermineFields(insertEvent.Properties)})";
        await mySqlQueryRepository.Execute(command, insertEvent.EventId);
    }

    private string DetermineFields(IDictionary<string, object> properties)
    {
        ICollection<string> resultArr = [];
        foreach (KeyValuePair<string, object> pair in properties)
        {
            resultArr.Add($"{pair.Key} {DetermineDataType(pair.Key, pair.Value)}");
        }

        resultArr.Add($"PRIMARY KEY (id)");

        return string.Join(',', resultArr);
    }

    private string DetermineDataType(string key, object value)
    {
        if (key.Contains("id")) return "VARCHAR(36)";
        if (value is not JsonElement jsonValue) return "VARCHAR(200)";

        return jsonValue.ValueKind switch
        {
            JsonValueKind.String => "VARCHAR(100)",
            JsonValueKind.Number => "INT",
            JsonValueKind.True => "BOOL",
            JsonValueKind.False => "BOOL",
            JsonValueKind.Object => "JSON",
            JsonValueKind.Array => "JSON",
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}
