using Application.Contracts.Events.Factory;
using Application.Contracts.Persistence;
using Infrastructure.Events.Mappings.MySQL;
using Infrastructure.Observer;
using Infrastructure.Persistence.CommandRepository;
using Infrastructure.Persistence.QueryRepository;
using Infrastructure.Projectors;
using Infrastructure.Recover;
using MongoDB.Bson;
using MongoDB.Driver;
using MySql.Data.MySqlClient;

namespace IntegrationTests.Projectors;

public class TestProjector
{
    private const string ConnectionStringToStartRepoMySql = "Server=localhost;Port=13306;User=root;Password=;";
    private const string ConnectionStringQueryRepoMySql = "Server=localhost;Port=13306;Database=cqrs_read;User=root;Password=;";
    private const string ConnectionStringCommandRepoMongo = "mongodb://localhost:27017/?connect=direct&replicaSet=rs0";

    [OneTimeSetUp]
    public async Task Set_DatabasesUp()
    {
        await using MySqlConnection connectionMySql = new MySqlConnection(ConnectionStringToStartRepoMySql);
        await connectionMySql.OpenAsync();

        string queryToStart = "CREATE DATABASE IF NOT EXISTS cqrs_read; USE cqrs_read; CREATE TABLE IF NOT EXISTS Products (product_id CHAR(36) PRIMARY KEY, name VARCHAR(255) NOT NULL, sku VARCHAR(100) NOT NULL, price DECIMAL(10,2) NOT NULL, stock_level INT NOT NULL, is_active BOOLEAN NOT NULL);CREATE TABLE IF NOT EXISTS last_info (id INT AUTO_INCREMENT PRIMARY KEY, last_event_id CHAR(36));INSERT IGNORE INTO last_info (id, last_event_id) VALUES (1, NULL);";

        await using MySqlCommand cmdGetLastEventId = new MySqlCommand(queryToStart, connectionMySql);
        await cmdGetLastEventId.ExecuteNonQueryAsync();
    }

    [TearDown]
    public async Task CleanUp()
    {
        await using MySqlConnection connectionMySql = new MySqlConnection(ConnectionStringQueryRepoMySql);
        await connectionMySql.OpenAsync();
        const string cleanupSql = "TRUNCATE TABLE Products; UPDATE last_info SET last_event_id = NULL WHERE id = 1;";
        await using MySqlCommand cmd = new MySqlCommand(cleanupSql, connectionMySql);
        await cmd.ExecuteNonQueryAsync();

        MongoClient client = new MongoClient(ConnectionStringCommandRepoMongo);
        IMongoDatabase? database = client.GetDatabase("users");
        IMongoCollection<BsonDocument>? collection = database.GetCollection<BsonDocument>("events");

        await collection.DeleteManyAsync(Builders<BsonDocument>.Filter.Empty);
    }

    [Test]
    public async Task Recover_Should_Handle_Events_That_Are_In_Outbox()
    {
        // Arrange
        ICommandRepository commandRepo = new MongoDbCommandRepository(ConnectionStringCommandRepoMongo);
        IQueryRepository queryRepo = new MySqlQueryRepository(ConnectionStringQueryRepoMySql);
        IEventFactory eventFactory = new MySqlEventFactory();

        Projector projector = new(commandRepo, queryRepo, eventFactory);

        // Act
        ICollection<string> eventsAdded = AddEventToOutbox();
        string lastEventId = ExtractEventId(eventsAdded.Last());

        Recovery recover = new Recovery(commandRepo, queryRepo, projector);
        recover.Recover();

        // Assert
        await AssertEventuallyAsync(async () =>
        {
            Guid dbEventId = await queryRepo.GetLastSuccessfulEventId();
            return dbEventId.ToString() == lastEventId;
        }, timeoutMs: 5000);
    }

    [Test]
    public async Task ChangeStream_Should_PickUp_New_Events()
    {
        // Arrange
        ICommandRepository commandRepo = new MongoDbCommandRepository(ConnectionStringCommandRepoMongo);
        IQueryRepository queryRepo = new MySqlQueryRepository(ConnectionStringQueryRepoMySql);
        IEventFactory eventFactory = new MySqlEventFactory();
        Projector projector = new Projector(commandRepo, queryRepo, eventFactory);
        MongoDbObserver observer = new MongoDbObserver(ConnectionStringCommandRepoMongo);

        using CancellationTokenSource cancellationToken = new CancellationTokenSource();
        Task observerTask = observer.StartListening(projector.AddEvent, cancellationToken.Token);

        // Act
        ICollection<string> events = AddEventToOutbox();
        string expectedId = ExtractEventId(events.Last());

        // Assert
        await AssertEventuallyAsync(async () =>
        {
            Guid dbEventId = await queryRepo.GetLastSuccessfulEventId();
            return dbEventId.ToString() == expectedId;
        }, 5000);

        await cancellationToken.CancelAsync();
        try { await observerTask; } catch (OperationCanceledException) { }
    }

    private string ExtractEventId(string json)
    {
        BsonDocument? doc = BsonDocument.Parse(json);
        return doc["event_id"].AsString;
    }

    private async Task AssertEventuallyAsync(Func<Task<bool>> condition, int timeoutMs)
    {
        DateTime start = DateTime.UtcNow;
        while ((DateTime.UtcNow - start).TotalMilliseconds < timeoutMs)
        {
            if (await condition()) return;
            await Task.Delay(100);
        }
    }

    private ICollection<string> AddEventToOutbox()
    {
        ICollection<string> events = new List<string>();
        MongoClient client = new MongoClient(ConnectionStringCommandRepoMongo);
        IMongoDatabase database = client.GetDatabase("users");
        IMongoCollection<BsonDocument> collection = database.GetCollection<BsonDocument>("events")!;

        for (int i = 0; i < 5; i++)
        {
            events.Add(
               $@"
                    {{
                      ""event_id"": ""{Guid.NewGuid()}"",
                      ""occured_at"": ""{DateTime.UtcNow:O}"",
                      ""aggregate_name"": ""Products"",
                      ""status"": ""PENDING"",
                      ""event_type"": ""INSERT"",
                      ""payload"": {{
                            ""product_id"": ""{Guid.NewGuid()}"",
                            ""name"": ""Test Product {i}"",
                            ""sku"": ""TEST-{i}"",    
                            ""price"": {10 + i},    
                            ""stock_level"": 100,
                            ""is_active"": true
                        }}
                    }}
                ");
        }

        foreach (string eventItem in events)
        {
            collection.InsertOne(BsonDocument.Parse(eventItem));
        }
        return events;
    }
}