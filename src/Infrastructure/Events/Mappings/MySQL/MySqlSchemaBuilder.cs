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

        PersistenceCommandInfo commandInfo = new PersistenceCommandInfo(command);
        await mySqlQueryRepository.Execute(commandInfo, insertEvent.EventId);
    }

    private bool DoesTableExists(string aggregateName)
    {
        return alreadyCreatedTables.Contains(aggregateName);
    }

    private string MapFields(IDictionary<string, object> properties)
    {
        ICollection<string> resultArr = [];
        foreach (KeyValuePair<string, object> pair in properties)
        {
            resultArr.Add($"{pair.Key} {DetermineDataType(pair.Key, pair.Value)}");
        }

        resultArr.Add($"PRIMARY KEY (id)");

        return string.Join(", ", resultArr);
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
}
