using Application.Contracts.Persistence;
using Infrastructure.Persistence.CommandRepository;
using IntegrationTests.Helpers;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;
using MongoDB.Driver;

namespace IntegrationTests.Persistence;

public class TestMongoDbCommandRepository
{
    private MongoDbCommandRepository _repository;
    private IMongoCollection<BsonDocument> _collection;

    [SetUp]
    public async Task SetUp()
    {
        MongoUrl url = new(TestConnectionStrings.MongoDbCommand);
        MongoClient client = new MongoClient(url);
        IMongoDatabase? database = client.GetDatabase(url.DatabaseName);
        _collection = database.GetCollection<BsonDocument>("events");

        await _collection.DeleteManyAsync(Builders<BsonDocument>.Filter.Empty);

        _repository = new MongoDbCommandRepository(TestConnectionStrings.MongoDbCommand, NullLogger<MongoDbCommandRepository>.Instance);
    }

    [TearDown]
    public async Task TearDown()
    {
        await _collection.DeleteManyAsync(Builders<BsonDocument>.Filter.Empty);
    }

    [Test]
    public async Task GetAllEvents_Should_Return_All_Events_Sorted_By_Id()
    {
        // Arrange
        List<BsonDocument> events = new List<BsonDocument>();
        for (int i = 0; i < 3; i++)
        {
            events.Add(BsonDocument.Parse($@"
            {{
                ""id"": ""{Guid.NewGuid()}"",
                ""occurredAt"": ""{DateTime.UtcNow:O}"",
                ""aggregateName"": ""TestAgg"",
                ""status"": ""PENDING"",
                ""eventType"": ""INSERT"",
                ""payload"": {{ ""index"": {i} }}
            }}"));
        }

        await _collection.InsertManyAsync(events);

        // Act
        ICollection<OutboxEvent> result = await _repository.GetAllEvents();

        // Assert
        Assert.That(result, Has.Count.EqualTo(3));
        Assert.That(result.First().EventItem, Does.Contain("\"index\" : 0"));
    }

    [Test]
    public async Task GetAllEvents_Should_Return_Events_Sorted_By_OccuredAt_When_Inserted_Sequentially()
    {
        // Arrange
        DateTime baseTime = DateTime.UtcNow;
        List<BsonDocument> events = new List<BsonDocument>();

        for (int i = 0; i < 3; i++)
        {
            DateTime time = baseTime.AddSeconds(i);
            events.Add(BsonDocument.Parse($@"
            {{
                ""id"": ""{Guid.NewGuid()}"",
                ""occurredAt"": ""{time:O}"",
                ""aggregateName"": ""TestAgg"",
                ""status"": ""PENDING"",
                ""eventType"": ""INSERT"",
                ""payload"": {{ ""sequence"": {i} }}
            }}"));
        }

        await _collection.InsertManyAsync(events);

        // Act
        ICollection<OutboxEvent> result = await _repository.GetAllEvents();

        // Assert
        Assert.That(result, Has.Count.EqualTo(3));

        List<DateTime> loadedDates = result.Select(e =>
        {
            BsonDocument? doc = BsonDocument.Parse(e.EventItem);
            return DateTime.Parse(doc["occurredAt"].AsString);
        }).ToList();

        Assert.That(loadedDates, Is.Ordered.Ascending);
    }

    [Test]
    public async Task RemoveEvent_Should_Delete_Event_From_Database()
    {
        // Arrange
        Guid eventId = Guid.NewGuid();
        BsonDocument? doc = BsonDocument.Parse($@"
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
        bool wasDeleted = await _repository.RemoveEvent(eventId);
        List<BsonDocument>? remainingEvents = await _collection.Find(new BsonDocument()).ToListAsync();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(wasDeleted, Is.True);
            Assert.That(remainingEvents, Is.Empty);
        });
    }

    [Test]
    public async Task RemoveEvent_Should_Return_False_If_Event_Does_Not_Exist()
    {
        // Act
        bool wasDeleted = await _repository.RemoveEvent(Guid.NewGuid());

        // Assert
        Assert.That(wasDeleted, Is.False);
    }
}