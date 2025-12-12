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

        SortDefinition<BsonDocument>? sort = Builders<BsonDocument>.Sort.Ascending("_id"); // occurredAt is niet nodig sinds _id ook met timestamp word generate, dus dit is al integrated.
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
        return result.IsAcknowledged && result.DeletedCount > 0;
    }
}