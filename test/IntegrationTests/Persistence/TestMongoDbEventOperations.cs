using Application.Contracts.Persistence;
using Infrastructure.Persistence.CommandRepository;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;
using MongoDB.Driver;

namespace IntegrationTests.Persistence;

public class TestMongoDbEventOperations
{
    private const string ConnectionStringCommandRepoMongo = "mongodb://localhost:27017/users?connect=direct&replicaSet=rs0";
    private MongoDbCommandRepository _repository;
    private IMongoCollection<BsonDocument> _collection;

    [SetUp]
    public async Task SetUp()
    {
        MongoUrl url = new(ConnectionStringCommandRepoMongo);
        MongoClient client = new MongoClient(url);
        IMongoDatabase? database = client.GetDatabase(url.DatabaseName);
        _collection = database.GetCollection<BsonDocument>("events");

        await _collection.DeleteManyAsync(Builders<BsonDocument>.Filter.Empty);

        _repository = new MongoDbCommandRepository(ConnectionStringCommandRepoMongo, NullLogger<MongoDbCommandRepository>.Instance);
    }

    [TearDown]
    public async Task TearDown()
    {
        await _collection.DeleteManyAsync(Builders<BsonDocument>.Filter.Empty);
    }

    [Test]
    public async Task MarkAsDone_Should_Update_Event_Status_To_DONE()
    {
        // Arrange
        Guid eventId = Guid.NewGuid();
        BsonDocument doc = BsonDocument.Parse($@"
            {{
                ""id"": ""{eventId}"",
                ""occurredAt"": ""{DateTime.UtcNow:O}"",
                ""aggregateName"": ""TestAgg"",
                ""status"": ""PENDING"",
                ""eventType"": ""INSERT"",
                ""payload"": {{}}
            }}");

        await _collection.InsertOneAsync(doc);

        // Act
        bool wasMarked = await _repository.MarkAsDone(eventId);

        // Assert
        Assert.That(wasMarked, Is.True);

        FilterDefinition<BsonDocument> filter = Builders<BsonDocument>.Filter.Eq("id", eventId.ToString());
        BsonDocument? updatedDoc = await _collection.Find(filter).FirstOrDefaultAsync();

        Assert.Multiple(() =>
        {
            Assert.That(updatedDoc, Is.Not.Null);
            Assert.That(updatedDoc["status"].AsString, Is.EqualTo("DONE"));
        });
    }
}
