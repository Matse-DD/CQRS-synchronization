using Application.Contracts.Events;
using Application.Contracts.Events.EventOptions;
using Application.Contracts.Persistence;
using System.Text.Json;

namespace Infrastructure.Events.Mappings.MySQL;

public class MySqlSchemaBuilder : ISchemaBuilder
{
    private readonly HashSet<string> _alreadyCreatedTables = new();

    public async Task Create(IQueryRepository mySqlQueryRepository, InsertEvent insertEvent)
    {
        string aggregateName = insertEvent.AggregateName;

        if (_alreadyCreatedTables.Contains(aggregateName)) return;

        string command = $"CREATE TABLE IF NOT EXISTS {aggregateName} ({MapFields(insertEvent.Properties)})";

        await mySqlQueryRepository.ExecuteSchemaCommand(command);
        _alreadyCreatedTables.Add(aggregateName);
    }

    private string MapFields(IDictionary<string, object> properties)
    {
        ICollection<string> resultArr = [];
        string? primaryKey = null;

        foreach (KeyValuePair<string, object> pair in properties)
        {
            resultArr.Add($"{pair.Key} {DetermineDataType(pair.Key, pair.Value)}");
            if (pair.Key.EndsWith("_id"))
            {
                primaryKey = pair.Key;
            }
        }

        if (primaryKey is not null)
        {
            resultArr.Add($"PRIMARY KEY ({primaryKey})");
        }

        return string.Join(", ", resultArr);
    }

    private string DetermineDataType(string key, object value)
    {
        if (key.Contains("_id")) return "VARCHAR(60)";
        if (value is not JsonElement jsonValue) return "VARCHAR(200)";

        return jsonValue.ValueKind switch
        {
            JsonValueKind.String => "VARCHAR(255)",
            JsonValueKind.Number => "FLOAT",
            JsonValueKind.True => "BOOL",
            JsonValueKind.False => "BOOL",
            JsonValueKind.Object => "JSON",
            JsonValueKind.Array => "JSON",
            _ => throw new ArgumentOutOfRangeException(nameof(value), "Unsupported JsonValueKind")
        };
    }
}