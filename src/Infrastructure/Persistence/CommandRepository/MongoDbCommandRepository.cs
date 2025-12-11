using Application.Contracts.Persistence;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Driver;

namespace Infrastructure.Persistence.CommandRepository;

public class MongoDbCommandRepository : ICommandRepository
{
    private readonly IMongoCollection<BsonDocument> _collection;

    public MongoDbCommandRepository(string connectionString)
    {
        MongoClient client = new(connectionString);
        IMongoDatabase database = client.GetDatabase("users"); //cqrs_command
        _collection = database.GetCollection<BsonDocument>("events")!;
    }

    public async Task<ICollection<OutboxEvent>> GetAllEvents()
    {

        SortDefinition<BsonDocument>? sort = Builders<BsonDocument>.Sort.Ascending("_id"); // occurred_at is niet nodig sinds _id ook met timestamp word generate, dus dit is al integrated.
        ICollection<BsonDocument> events = await _collection
            .Find(_ => true)
            .Sort(sort)
            .ToListAsync();

        ICollection<OutboxEvent> outboxEvents = events.Select(
            d =>
            {
                return new OutboxEvent(d.GetValue("id").AsString ?? string.Empty,

                ConvertToPureBSON(d).ToJson() ?? string.Empty);
            }
        ).ToList();

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

    private static BsonDocument ConvertToPureBSON(BsonDocument doc) // TODO this method is double with the one in the mongodb observer
    {
        BsonDocument clone = new BsonDocument(doc);

        if (clone.Contains("occurredAt") && clone["occurredAt"].IsBsonDateTime)
        {
            string dt = clone["occurredAt"].ToUniversalTime().ToString("o");
            clone["occurredAt"] = new BsonString(dt);
        }

        return clone;
    }

}