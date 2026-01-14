using MongoDB.Bson;

namespace IntegrationTests.Helpers;

public class BsonEventBuilder
{
    private Guid _id = Guid.NewGuid();
    private DateTime _occurredAt = DateTime.UtcNow;
    private string _aggregateName = "TestAggregate";
    private string _status = "PENDING";
    private string _eventType = "INSERT";
    private BsonDocument _payload = new();

    public static BsonEventBuilder Create() => new();

    public BsonEventBuilder WithId(Guid id)
    {
        _id = id;
        return this;
    }

    public BsonEventBuilder WithOccurredAt(DateTime occurredAt)
    {
        _occurredAt = occurredAt;
        return this;
    }

    public BsonEventBuilder WithAggregateName(string aggregateName)
    {
        _aggregateName = aggregateName;
        return this;
    }

    public BsonEventBuilder WithStatus(string status)
    {
        _status = status;
        return this;
    }

    public BsonEventBuilder WithEventType(string eventType)
    {
        _eventType = eventType;
        return this;
    }

    public BsonEventBuilder WithPayload(BsonDocument payload)
    {
        _payload = payload;
        return this;
    }

    public BsonEventBuilder WithInsertPayload(Dictionary<string, object> fields)
    {
        _eventType = "INSERT";
        _payload = new BsonDocument(fields.Select(kvp => new BsonElement(kvp.Key, BsonValue.Create(kvp.Value))));
        return this;
    }

        public BsonEventBuilder WithUpdatePayload(Dictionary<string, object> change, Dictionary<string, object> condition)
    {
        _eventType = "UPDATE";
        _payload = new BsonDocument
        {
            { "change", new BsonDocument(change.Select(kvp => new BsonElement(kvp.Key, BsonValue.Create(kvp.Value)))) },
            { "condition", new BsonDocument(condition.Select(kvp => new BsonElement(kvp.Key, BsonValue.Create(kvp.Value)))) }
        };
        return this;
    }
    public BsonEventBuilder WithDeletePayload(Dictionary<string, object> condition)
    {
        _eventType = "DELETE";
        _payload = new BsonDocument
        {
            { "condition", new BsonDocument(condition.Select(kvp => new BsonElement(kvp.Key, BsonValue.Create(kvp.Value)))) }
        };
        return this;
    }

    public BsonDocument Build()
    {
        return new BsonDocument
        {
            { "id", _id.ToString() },
            { "occurredAt", _occurredAt.ToString("O") },
            { "aggregateName", _aggregateName },
            { "status", _status },
            { "eventType", _eventType },
            { "payload", _payload }
        };
    }

    public string BuildAsJson()
    {
        return Build().ToJson();
    }

    public static BsonDocument CreateInsertEvent(
        string aggregateName,
        Dictionary<string, object> payload,
        Guid? id = null,
        string status = "PENDING")
    {
        return Create()
            .WithId(id ?? Guid.NewGuid())
            .WithAggregateName(aggregateName)
            .WithStatus(status)
            .WithInsertPayload(payload)
            .Build();
    }

     public static BsonDocument CreateUpdateEvent(
        string aggregateName,
        Dictionary<string, object> change,
        Dictionary<string, object> condition,
        Guid? id = null,
        string status = "PENDING")
    {
        return Create()
            .WithId(id ?? Guid.NewGuid())
            .WithAggregateName(aggregateName)
            .WithStatus(status)
            .WithUpdatePayload(change, condition)
            .Build();
    }
}