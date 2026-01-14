using Application.Contracts.Events;
using Application.Contracts.Events.EventOptions;
using Application.Contracts.Persistence;
using Infrastructure.Persistence;
using System.Text.Json;

namespace Infrastructure.Events.Mappings.MySQL;

public class MySqlSchemaBuilder : ISchemaBuilder
{
    private readonly HashSet<string> alreadyCreatedTables = new HashSet<string>();

    public async Task Create(IQueryRepository mySqlQueryRepository, InsertEvent insertEvent)
    {
        string aggregateName = insertEvent.AggregateName;

        if (DoesTableExists(aggregateName)) return;

        string command = $"CREATE TABLE IF NOT EXISTS {aggregateName} ({MapFields(insertEvent.Properties)})";

        CommandInfo commandInfo = new CommandInfo(command);
        await mySqlQueryRepository.Execute(commandInfo, insertEvent.EventId);

        alreadyCreatedTables.Add(aggregateName);
    }

    private bool DoesTableExists(string aggregateName)
    {
        return alreadyCreatedTables.Contains(aggregateName);
    }

    private string MapFields(IDictionary<string, object> properties)
    {
        List<string> mappedColumns = new();

        foreach (KeyValuePair<string, object> pair in properties)
        {
            mappedColumns.Add($"{pair.Key} {DetermineDataType(pair.Key, pair.Value)}");
        }

        string primaryKey = DeterminePrimaryKey(properties.Keys);
        if (!properties.ContainsKey(primaryKey))
        {
            mappedColumns.Insert(0, $"{primaryKey} VARCHAR(60)");
        }

        mappedColumns.Add($"PRIMARY KEY ({primaryKey})");

        return string.Join(", ", mappedColumns);
    }

    private string DetermineDataType(string key, object value)
    {
        if (key.Contains("id")) return "VARCHAR(60)";
        if (value is not JsonElement jsonValue) return "VARCHAR(200)";

        return jsonValue.ValueKind switch
        {
            JsonValueKind.String => "VARCHAR(255)",
            JsonValueKind.Number => "FLOAT",
            JsonValueKind.True => "BOOL",
            JsonValueKind.False => "BOOL",
            JsonValueKind.Object => "JSON",
            JsonValueKind.Array => "JSON",
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    private static string DeterminePrimaryKey(IEnumerable<string> keys)
    {
        string? exactId = keys.FirstOrDefault(key => string.Equals(key, "id", StringComparison.OrdinalIgnoreCase));
        if (exactId != null)
        {
            return exactId;
        }

        string? suffixedId = keys.FirstOrDefault(key => key.EndsWith("_id", StringComparison.OrdinalIgnoreCase));
        if (suffixedId != null)
        {
            return suffixedId;
        }

        string? firstKey = keys.FirstOrDefault();
        return string.IsNullOrWhiteSpace(firstKey) ? "id" : firstKey;
    }
}
