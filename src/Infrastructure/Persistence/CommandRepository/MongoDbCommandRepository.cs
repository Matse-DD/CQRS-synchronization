using Application.Contracts.Persistence;
using Infrastructure.Tools.DatabaseExtensions;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Infrastructure.Persistence.CommandRepository;

public class MongoDbCommandRepository : ICommandRepository
{
    private readonly IMongoCollection<BsonDocument> _collection;
    private readonly ILogger<MongoDbCommandRepository> _logger;

    public MongoDbCommandRepository(string connectionString, ILogger<MongoDbCommandRepository> logger)
    {
        MongoUrl url = new MongoUrl(connectionString);
        MongoClient client = new(url);

        string databaseName = url.DatabaseName ?? throw new ArgumentException("Connection string does not contain database name");
        IMongoDatabase database = client.GetDatabase(databaseName);

        _collection = database.GetCollection<BsonDocument>("events")!;
        _logger = logger;
    }


    public async Task<ICollection<OutboxEvent>> GetAllEvents()
    {
        ICollection<BsonDocument> events = await RequestEvents();

        ICollection<OutboxEvent> outboxEvents = events.Select(MapToOutboxEvent).ToList();

        return outboxEvents;
    }


    public async Task<bool> RemoveEvent(Guid eventId)
    {
        FilterDefinition<BsonDocument> filter = Builders<BsonDocument>.Filter.Eq(
            "id", eventId.ToString()
        );

        DeleteResult result = await _collection.DeleteOneAsync(filter);

        if (result.DeletedCount > 0)
        {
            _logger.LogInformation("Removed event {EventId} from Outbox", eventId);
        }

        return result.IsAcknowledged && result.DeletedCount > 0;
    }

    public async Task<bool> MarkAsDone(Guid eventId)
    {
        FilterDefinition<BsonDocument> filter = Builders<BsonDocument>.Filter.Eq("id", eventId.ToString());

        UpdateResult result = await _collection.UpdateOneAsync(filter, Builders<BsonDocument>.Update.Set("status", "DONE"));

        if (result.ModifiedCount > 0)
        {
            _logger.LogInformation("Marked event {EventId} as DONE", eventId);
        }

        return result is { IsAcknowledged: true, ModifiedCount: > 0 };
    }

    private OutboxEvent MapToOutboxEvent(BsonDocument incomingEventItem)
    {
        string eventId = incomingEventItem.GetValue("id").AsString;
        string eventItem = incomingEventItem.SanitizeOccurredAt().ToJson();

        return new OutboxEvent(eventId ?? string.Empty, eventItem ?? string.Empty);
    }

    private async Task<ICollection<BsonDocument>> RequestEvents()
    {
        SortDefinition<BsonDocument>? sort = Builders<BsonDocument>.Sort.Ascending("occurredAt");

        ICollection<BsonDocument> events = await _collection
            .Find(_ => true)
            .Sort(sort)
            .ToListAsync();

        return events;
    }
}