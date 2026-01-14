using Application.Contracts.Persistence;
using Infrastructure.Persistence.CommandRepository;
using IntegrationTests.Helpers;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;
using MongoDB.Driver;

namespace IntegrationTests.Persistence;

public class TestMongoDbEventOperations
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
    public async Task MarkAsDone_Should_Update_Event_Status_To_DONE()
    {
        // Arrange
        Guid eventId = Guid.NewGuid();
        BsonDocument doc = BsonEventBuilder.Create()
            .WithId(eventId)
            .WithAggregateName("TestAgg")
            .WithStatus("PENDING")
            .WithEventType("INSERT")
            .WithPayload(new BsonDocument())
            .Build();

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
            BsonEventBuilder.CreateInsertEvent("Product",
                new Dictionary<string, object> { { "name", "Product1" } }),

            BsonEventBuilder.Create()
                .WithOccurredAt(baseTime.AddSeconds(1))
                .WithUpdatePayload(
                    new Dictionary<string, object> { { "price", "100" } },
                    new Dictionary<string, object> { { "id", "1" } })
                .WithAggregateName("Product")
                .Build(),

            BsonEventBuilder.Create()
                .WithOccurredAt(baseTime.AddSeconds(2))
                .WithDeletePayload(new Dictionary<string, object> { { "id", "2" } })
                .WithAggregateName("Product")
                .WithStatus("DONE")
                .Build()
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
            BsonEventBuilder.CreateInsertEvent("TestAgg", new Dictionary<string, object>(), eventId1),
            BsonEventBuilder.CreateInsertEvent("TestAgg", new Dictionary<string, object>(), eventId2)
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
        BsonDocument doc = BsonEventBuilder.CreateInsertEvent("TestAgg", new Dictionary<string, object>(), eventId);

        await _collection.InsertOneAsync(doc);

        // Act - Create new repository instance
        MongoDbCommandRepository newRepository = new(TestConnectionStrings.MongoDbCommand, NullLogger<MongoDbCommandRepository>.Instance);
        ICollection<OutboxEvent> events = await newRepository.GetAllEvents();

        // Assert
        Assert.That(events, Has.Count.EqualTo(1));
        Assert.That(events.First().EventId, Is.EqualTo(eventId.ToString()));
    }
}
