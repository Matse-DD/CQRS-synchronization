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

    [Test]
    public async Task MarkAsDone_Should_Return_False_When_Event_Does_Not_Exist()
    {
        // Act
        bool wasMarked = await _repository.MarkAsDone(Guid.NewGuid());

        // Assert
        Assert.That(wasMarked, Is.False);
    }
    
    [Test]
    public async Task GetAllEvents_Should_Return_Empty_Collection_When_No_Events_Exist()
    {
        // Act
        ICollection<OutboxEvent> result = await _repository.GetAllEvents();

        // Assert
        Assert.That(result, Is.Empty);
    }
    
    [Test]
    public async Task GetAllEvents_Should_Handle_Multiple_Event_Types()
    {
        // Arrange
        DateTime baseTime = DateTime.UtcNow;
        List<BsonDocument> events =
        [
            BsonDocument.Parse($@"
            {{
                ""id"": ""{Guid.NewGuid()}"",
                ""occurredAt"": ""{baseTime:O}"",
                ""aggregateName"": ""Product"",
                ""status"": ""PENDING"",
                ""eventType"": ""INSERT"",
                ""payload"": {{ ""name"": ""Product1"" }}
            }}"),

            BsonDocument.Parse($@"
            {{
                ""id"": ""{Guid.NewGuid()}"",
                ""occurredAt"": ""{baseTime.AddSeconds(1):O}"",
                ""aggregateName"": ""Product"",
                ""status"": ""PENDING"",
                ""eventType"": ""UPDATE"",
                ""payload"": {{ ""change"": {{ ""price"": ""100"" }}, ""condition"": {{ ""id"": ""1"" }} }}
            }}"),

            BsonDocument.Parse($@"
            {{
                ""id"": ""{Guid.NewGuid()}"",
                ""occurredAt"": ""{baseTime.AddSeconds(2):O}"",
                ""aggregateName"": ""Product"",
                ""status"": ""DONE"",
                ""eventType"": ""DELETE"",
                ""payload"": {{ ""condition"": {{ ""id"": ""2"" }} }}
            }}")
        ];

        await _collection.InsertManyAsync(events);

        // Act
        ICollection<OutboxEvent> result = await _repository.GetAllEvents();

        // Assert
        Assert.That(result, Has.Count.EqualTo(3));
        Assert.That(result.Select(e => BsonDocument.Parse(e.EventItem)["eventType"].AsString), 
            Is.EquivalentTo(new[] { "INSERT", "UPDATE", "DELETE" }));
    }

    [Test]
    public async Task RemoveEvent_Should_Only_Delete_Specified_Event()
    {
        // Arrange
        Guid eventId1 = Guid.NewGuid();
        Guid eventId2 = Guid.NewGuid();

        await _collection.InsertManyAsync(new[]
        {
            BsonDocument.Parse($@"
            {{
                ""id"": ""{eventId1}"",
                ""occurredAt"": ""{DateTime.UtcNow:O}"",
                ""aggregateName"": ""TestAgg"",
                ""status"": ""PENDING"",
                ""eventType"": ""INSERT"",
                ""payload"": {{}}
            }}"),
            BsonDocument.Parse($@"
            {{
                ""id"": ""{eventId2}"",
                ""occurredAt"": ""{DateTime.UtcNow:O}"",
                ""aggregateName"": ""TestAgg"",
                ""status"": ""PENDING"",
                ""eventType"": ""INSERT"",
                ""payload"": {{}}
            }}")
        });

        // Act
        bool wasDeleted = await _repository.RemoveEvent(eventId1);

        // Assert
        Assert.That(wasDeleted, Is.True);

        List<BsonDocument> remainingEvents = await _collection.Find(new BsonDocument()).ToListAsync();
        Assert.Multiple(() =>
        {
            Assert.That(remainingEvents, Has.Count.EqualTo(1));
            Assert.That(remainingEvents[0]["id"].AsString, Is.EqualTo(eventId2.ToString()));
        });
    }
    
    [Test]
    public async Task Events_Should_Be_Persisted_Across_Repository_Instances()
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

        // Act - Create new repository instance
        MongoDbCommandRepository newRepository = new(ConnectionStringCommandRepoMongo, NullLogger<MongoDbCommandRepository>.Instance);
        ICollection<OutboxEvent> events = await newRepository.GetAllEvents();

        // Assert
        Assert.That(events, Has.Count.EqualTo(1));
        Assert.That(events.First().EventId, Is.EqualTo(eventId.ToString()));
    }
}
