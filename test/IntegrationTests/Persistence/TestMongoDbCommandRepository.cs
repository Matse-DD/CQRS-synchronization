using Application.Contracts.Persistence;
using Infrastructure.Persistence.CommandRepository;
using MongoDB.Bson;
using MongoDB.Driver;

namespace IntegrationTests.Persistence;

public class TestMongoDbCommandRepository
{
    private const string ConnectionStringCommandRepoMongo = "mongodb://localhost:27017/?connect=direct&replicaSet=rs0";
    private MongoDbCommandRepository _repository;
    private IMongoCollection<BsonDocument> _collection;

    [SetUp]
    public async Task SetUp()
    {
        MongoClient client = new MongoClient(ConnectionStringCommandRepoMongo);
        IMongoDatabase? database = client.GetDatabase("users");
        _collection = database.GetCollection<BsonDocument>("events");

        await _collection.DeleteManyAsync(Builders<BsonDocument>.Filter.Empty);

        _repository = new MongoDbCommandRepository(ConnectionStringCommandRepoMongo);
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
                ""event_id"": ""{Guid.NewGuid()}"",
                ""occured_at"": ""{DateTime.UtcNow:O}"",
                ""aggregate_name"": ""TestAgg"",
                ""status"": ""PENDING"",
                ""event_type"": ""INSERT"",
                ""payload"": {{ ""index"": {i} }}
            }}"));
        }
        
        await _collection.InsertManyAsync(events);

        // Act
        ICollection<OutboxEvent> result = await _repository.GetAllEvents();

        // Assert
        Assert.That(result, Has.Count.EqualTo(3));
        Assert.That(result.First().eventItem, Does.Contain("\"index\" : 0")); 
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
                ""event_id"": ""{Guid.NewGuid()}"",
                ""occured_at"": ""{time:O}"",
                ""aggregate_name"": ""TestAgg"",
                ""status"": ""PENDING"",
                ""event_type"": ""INSERT"",
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
            BsonDocument? doc = BsonDocument.Parse(e.eventItem);
            return DateTime.Parse(doc["occured_at"].AsString);
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
                ""event_id"": ""{eventId}"",
                ""occured_at"": ""{DateTime.UtcNow:O}"",
                ""aggregate_name"": ""TestAgg"",
                ""status"": ""PENDING"",
                ""event_type"": ""INSERT"",
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