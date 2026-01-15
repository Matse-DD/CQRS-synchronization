using Application.Contracts.Events;
using Application.Contracts.Events.Factory;
using Application.Contracts.Persistence;
using Infrastructure.Events.Mappings.MySQL;
using Infrastructure.Observer;
using Infrastructure.Persistence.CommandRepository;
using Infrastructure.Persistence.QueryRepository;
using Infrastructure.Projectors;
using Infrastructure.Recover;
using Infrastructure.Replay;
using IntegrationTests.Helpers;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;
using MongoDB.Driver;
using MySql.Data.MySqlClient;

namespace IntegrationTests.Projectors;

public class TestProjector
{
    [OneTimeSetUp]
    public async Task Set_DatabasesUp()
    {
        await using MySqlConnection connectionMySql = new MySqlConnection(TestConnectionStrings.MySqlSetup);
        await connectionMySql.OpenAsync();

        string queryToStart = "CREATE DATABASE IF NOT EXISTS cqrs_read; USE cqrs_read;";

        await using MySqlCommand cmdGetLastEventId = new MySqlCommand(queryToStart, connectionMySql);
        await cmdGetLastEventId.ExecuteNonQueryAsync();
    }

    [TearDown]
    public async Task CleanUp()
    {
        await using MySqlConnection connectionMySql = new MySqlConnection(TestConnectionStrings.MySqlQuery);
        await connectionMySql.OpenAsync();
        const string cleanupSql = "DROP TABLE IF EXISTS Products; UPDATE last_info SET last_event_id = NULL WHERE id = 1";
        await using MySqlCommand cmd = new MySqlCommand(cleanupSql, connectionMySql);
        await cmd.ExecuteNonQueryAsync();

        MongoUrl mongoUrl = new(TestConnectionStrings.MongoDbCommand);
        MongoClient client = new MongoClient(mongoUrl);
        IMongoDatabase? database = client.GetDatabase(mongoUrl.DatabaseName);
        IMongoCollection<BsonDocument>? collection = database.GetCollection<BsonDocument>("events");

        await collection.DeleteManyAsync(Builders<BsonDocument>.Filter.Empty);
    }

    [Test]
    public async Task Recover_Should_Handle_Events_That_Are_In_Outbox()
    {
        // Arrange
        ICommandRepository commandRepo = new MongoDbCommandRepository(TestConnectionStrings.MongoDbCommand, NullLogger<MongoDbCommandRepository>.Instance);
        IQueryRepository queryRepo = new MySqlQueryRepository(TestConnectionStrings.MySqlQuery, NullLogger<MySqlQueryRepository>.Instance);
        IEventFactory eventFactory = new MySqlEventFactory();
        ISchemaBuilder schemaBuilder = new MySqlSchemaBuilder();

        Projector projector = new(commandRepo, queryRepo, eventFactory, NullLogger<Projector>.Instance, schemaBuilder);


        // Act
        ICollection<string> eventsAdded = AddEventToOutbox();
        string lastEventId = ExtractEventId(eventsAdded.Last());

        Recovery recover = new Recovery(commandRepo, queryRepo, projector, NullLogger<Recovery>.Instance);
        _ = recover.Recover();

        // Assert
        await AssertEventuallyAsync(async () =>
        {
            Guid dbEventId = await queryRepo.GetLastSuccessfulEventId();
            return dbEventId.ToString() == lastEventId;
        }, timeoutMs: 5000);
    }

    [Test]
    public async Task Replay_Should_Handle_Events_That_Are_In_Outbox()
    {
        //Arrange
        ICommandRepository commandRepository = new MongoDbCommandRepository(TestConnectionStrings.MongoDbCommand, NullLogger<MongoDbCommandRepository>.Instance);
        IQueryRepository queryRepository = new MySqlQueryRepository(TestConnectionStrings.MySqlQuery, NullLogger<MySqlQueryRepository>.Instance);
        IEventFactory eventFactory = new MySqlEventFactory();
        ISchemaBuilder schemaBuilder = new MySqlSchemaBuilder();

        Projector projector = new(commandRepository, queryRepository, eventFactory, NullLogger<Projector>.Instance, schemaBuilder);

        //Act
        ICollection<string> eventsAdded = AddEventToOutbox();
        string lastEventId = ExtractEventId(eventsAdded.Last());

        Replayer replay = new(commandRepository, queryRepository, projector, NullLogger<Replayer>.Instance);
        replay.Replay();
        //Assert
        await AssertEventuallyAsync(async () =>
        {
            Guid dbEventId = await queryRepository.GetLastSuccessfulEventId();
            return dbEventId.ToString() == lastEventId;
        }, 5000);
    }

    [Test]
    public async Task ChangeStream_Should_PickUp_New_Events()
    {
        // Arrange
        ICommandRepository commandRepo = new MongoDbCommandRepository(TestConnectionStrings.MongoDbCommand, NullLogger<MongoDbCommandRepository>.Instance);
        IQueryRepository queryRepo = new MySqlQueryRepository(TestConnectionStrings.MySqlQuery, NullLogger<MySqlQueryRepository>.Instance);
        IEventFactory eventFactory = new MySqlEventFactory();
        ISchemaBuilder schemaBuilder = new MySqlSchemaBuilder();

        Projector projector = new Projector(commandRepo, queryRepo, eventFactory, NullLogger<Projector>.Instance, schemaBuilder);
        MongoDbObserver observer = new MongoDbObserver(TestConnectionStrings.MongoDbCommand, NullLogger<MongoDbObserver>.Instance);

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
        return doc["id"].AsString;
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
        MongoUrl url = new(TestConnectionStrings.MongoDbCommand);
        MongoClient client = new MongoClient(url);
        IMongoDatabase database = client.GetDatabase(url.DatabaseName);
        IMongoCollection<BsonDocument> collection = database.GetCollection<BsonDocument>("events")!;

        for (int i = 0; i < 5; i++)
        {
            events.Add(
               $@"
                    {{
                      ""id"": ""{Guid.NewGuid()}"",
                      ""occurredAt"": ""{DateTime.UtcNow:O}"",
                      ""aggregateName"": ""Products"",
                      ""status"": ""PENDING"",
                      ""eventType"": ""INSERT"",
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