using Application.Contracts.Persistence;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Threading.Tasks;

namespace Infrastructure.Persistence.CommandRepository;

public class MongoDbCommandRepository : ICommandRepository
{
    private readonly MongoClient _client;
    private readonly IMongoDatabase _database;
    private readonly IMongoCollection<BsonDocument> _collection;

    public MongoDbCommandRepository(string connectionString)
    {
        _client = new MongoClient(connectionString);
        _database = _client.GetDatabase("users"); //cqrs_command
        _collection = _database.GetCollection<BsonDocument>("events")!;
    }

    public async Task<ICollection<OutboxEvent>> GetAllEvents()
    {
        ICollection<BsonDocument> events = await _collection.Find(_ => true).ToListAsync();
        ICollection<OutboxEvent> outboxEvents = events.Select(
            d => new OutboxEvent(d.GetValue("event_id").AsString ?? string.Empty,
            d.ToJson() ?? string.Empty)
        ).ToList();

        return outboxEvents;
    }

    public async Task<bool> RemoveEvent(Guid eventId)
    {
        FilterDefinition<BsonDocument> filter = Builders<BsonDocument>.Filter.Eq(
            "event_id", eventId.ToString()
        );

        DeleteResult result = await _collection.DeleteOneAsync(filter);
        return result.IsAcknowledged && result.DeletedCount > 0;
    }
}
