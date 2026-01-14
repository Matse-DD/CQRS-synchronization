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
}