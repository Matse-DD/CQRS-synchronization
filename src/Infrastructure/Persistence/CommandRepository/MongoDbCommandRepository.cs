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
        MongoClient client = new(connectionString);
        IMongoDatabase database = client.GetDatabase("users"); //cqrs_command
        _collection = database.GetCollection<BsonDocument>("events")!;
        _logger = logger;
    }

    public async Task<ICollection<OutboxEvent>> GetAllEvents()
    {
        SortDefinition<BsonDocument>? sort = Builders<BsonDocument>.Sort.Ascending("_id");
        ICollection<BsonDocument> events = await _collection
            .Find(_ => true)
            .Sort(sort)
            .ToListAsync();

        ICollection<OutboxEvent> outboxEvents = events.Select(d =>
            new OutboxEvent(d.GetValue("id").AsString ?? string.Empty, d.SanitizeOccurredAt().ToJson() ?? string.Empty)).ToList();

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

        return result is { IsAcknowledged: true, DeletedCount: > 0 };
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
}