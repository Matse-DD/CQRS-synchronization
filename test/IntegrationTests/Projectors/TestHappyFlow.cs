using Application.Contracts.Events;
using Application.Contracts.Events.Factory;
using Application.Contracts.Persistence;
using Infrastructure.Events.Mappings.MySQL;
using Infrastructure.Persistence.CommandRepository;
using Infrastructure.Persistence.QueryRepository;
using Infrastructure.Projectors;
using IntegrationTests.Helpers;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;
using MongoDB.Driver;
using MySql.Data.MySqlClient;

namespace IntegrationTests.Projectors;

public class TestHappyFlow
{
    private const string ConnectionStringToStartRepoMySql = "Server=localhost;Port=13306;User=root;Password=;";
    private const string ConnectionStringQueryRepoMySql = "Server=localhost;Port=13306;Database=cqrs_read;User=root;Password=;";
    private const string ConnectionStringCommandRepoMongo = "mongodb://localhost:27017/users?connect=direct&replicaSet=rs0";

    private ICommandRepository _commandRepo;
    private IQueryRepository _queryRepo;
    private Projector _projector;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        await using MySqlConnection connectionMySql = new MySqlConnection(ConnectionStringToStartRepoMySql);
        await connectionMySql.OpenAsync();

        string queryToStart = "CREATE DATABASE IF NOT EXISTS cqrs_read; USE cqrs_read;";

        await using MySqlCommand cmdGetLastEventId = new MySqlCommand(queryToStart, connectionMySql);
        await cmdGetLastEventId.ExecuteNonQueryAsync();
    }

    [SetUp]
    public async Task SetUp()
    {
        await using MySqlConnection connectionMySql = new MySqlConnection(ConnectionStringQueryRepoMySql);
        await connectionMySql.OpenAsync();
        const string cleanupSql = "DROP TABLE IF EXISTS Products; UPDATE last_info SET last_event_id = NULL WHERE id = 1";
        await using MySqlCommand cmd = new MySqlCommand(cleanupSql, connectionMySql);
        await cmd.ExecuteNonQueryAsync();

        MongoUrl mongoUrl = new(ConnectionStringCommandRepoMongo);
        MongoClient client = new MongoClient(mongoUrl);
        IMongoDatabase? database = client.GetDatabase(mongoUrl.DatabaseName);
        IMongoCollection<BsonDocument>? collection = database.GetCollection<BsonDocument>("events");
        await collection.DeleteManyAsync(Builders<BsonDocument>.Filter.Empty);

        _commandRepo = new MongoDbCommandRepository(ConnectionStringCommandRepoMongo, NullLogger<MongoDbCommandRepository>.Instance);
        _queryRepo = new MySqlQueryRepository(ConnectionStringQueryRepoMySql, NullLogger<MySqlQueryRepository>.Instance);
        IEventFactory eventFactory = new MySqlEventFactory();
        ISchemaBuilder schemaBuilder = new MySqlSchemaBuilder();

        _projector = new Projector(_commandRepo, _queryRepo, eventFactory, NullLogger<Projector>.Instance, schemaBuilder);
    }

    [TearDown]
    public async Task TearDown()
    {
        await using MySqlConnection connectionMySql = new MySqlConnection(ConnectionStringQueryRepoMySql);
        await connectionMySql.OpenAsync();
        const string cleanupSql = "DROP TABLE IF EXISTS Products; UPDATE last_info SET last_event_id = NULL WHERE id = 1";
        await using MySqlCommand cmd = new MySqlCommand(cleanupSql, connectionMySql);
        await cmd.ExecuteNonQueryAsync();

        MongoUrl mongoUrl = new(ConnectionStringCommandRepoMongo);
        MongoClient client = new MongoClient(mongoUrl);
        IMongoDatabase? database = client.GetDatabase(mongoUrl.DatabaseName);
        IMongoCollection<BsonDocument>? collection = database.GetCollection<BsonDocument>("events");
        await collection.DeleteManyAsync(Builders<BsonDocument>.Filter.Empty);
    }

    [Test]
    public async Task HappyFlow_INSERT_Should_Sync_From_MongoDB_To_MySQL()
    {
        // Arrange
        Guid eventId = Guid.NewGuid();
        Guid productId = Guid.NewGuid();
        string productName = "Happy Flow Test Product";
        string productSku = "HFT-001";
        double productPrice = 99.99;
        int stockLevel = 50;

        BsonDocument insertEvent = BsonEventBuilder.Create()
            .WithId(eventId)
            .WithOccurredAt(DateTime.UtcNow)
            .WithAggregateName("Products")
            .WithStatus("PENDING")
            .WithInsertPayload(new Dictionary<string, object>
            {
                { "product_id", productId.ToString() },
                { "name", productName },
                { "sku", productSku },
                { "price", productPrice },
                { "stock_level", stockLevel },
                { "is_active", true }
            })
            .Build();

        // Act
        MongoUrl url = new(ConnectionStringCommandRepoMongo);
        MongoClient client = new MongoClient(url);
        IMongoDatabase database = client.GetDatabase(url.DatabaseName);
        IMongoCollection<BsonDocument> collection = database.GetCollection<BsonDocument>("events");
        await collection.InsertOneAsync(insertEvent);
        _projector.AddEvent(insertEvent.ToJson());
        await Task.Delay(500);

        // Assert
        await AssertEventuallyAsync(async () =>
        {
            try
            {
                await using MySqlConnection connection = new MySqlConnection(ConnectionStringQueryRepoMySql);
                await connection.OpenAsync();

                string query = "SELECT COUNT(*) FROM Products WHERE product_id = @productId AND name = @name AND sku = @sku AND price = @price AND stock_level = @stockLevel";
                await using MySqlCommand cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@productId", productId.ToString());
                cmd.Parameters.AddWithValue("@name", productName);
                cmd.Parameters.AddWithValue("@sku", productSku);
                cmd.Parameters.AddWithValue("@price", productPrice);
                cmd.Parameters.AddWithValue("@stockLevel", stockLevel);

                long count = (long)(await cmd.ExecuteScalarAsync())!;
                return count == 1;
            }
            catch (MySqlException)
            {
                return false;
            }
        }, timeoutMs: 10000);

        await AssertEventuallyAsync(async () =>
        {
            Guid lastEventId = await _queryRepo.GetLastSuccessfulEventId();
            return lastEventId == eventId;
        }, timeoutMs: 5000);
    }

    [Test]
    public async Task HappyFlow_UPDATE_Should_Sync_From_MongoDB_To_MySQL()
    {
        // Arrange
        Guid insertEventId = Guid.NewGuid();
        Guid productId = Guid.NewGuid();
        string originalName = "Original Product";
        decimal originalPrice = 50.00m;

        BsonDocument insertEvent = BsonEventBuilder.Create()
            .WithId(insertEventId)
            .WithOccurredAt(DateTime.UtcNow)
            .WithAggregateName("Products")
            .WithStatus("PENDING")
            .WithInsertPayload(new Dictionary<string, object>
            {
                { "product_id", productId.ToString() },
                { "name", originalName },
                { "sku", "UPDATE-TEST" },
                { "price", originalPrice },
                { "stock_level", 100 },
                { "is_active", true }
            })
            .Build();

        MongoUrl url = new(ConnectionStringCommandRepoMongo);
        MongoClient client = new MongoClient(url);
        IMongoDatabase database = client.GetDatabase(url.DatabaseName);
        IMongoCollection<BsonDocument> collection = database.GetCollection<BsonDocument>("events");
        await collection.InsertOneAsync(insertEvent);
        _projector.AddEvent(insertEvent.ToJson());
        await Task.Delay(500);

        await AssertEventuallyAsync(async () =>
        {
            Guid lastEventId = await _queryRepo.GetLastSuccessfulEventId();
            return lastEventId == insertEventId;
        }, timeoutMs: 5000);

        // Act
        Guid updateEventId = Guid.NewGuid();
        string updatedName = "Updated Product Name";
        double updatedPrice = 75.50;

        BsonDocument updateEvent = BsonEventBuilder.Create()
            .WithId(updateEventId)
            .WithOccurredAt(DateTime.UtcNow)
            .WithAggregateName("Products")
            .WithStatus("PENDING")
            .WithUpdatePayload(
                change: new Dictionary<string, object>
                {
                    { "name", updatedName },
                    { "price", updatedPrice }
                },
                condition: new Dictionary<string, object>
                {
                    { "product_id", productId.ToString() }
                })
            .Build();

        await collection.InsertOneAsync(updateEvent);
        _projector.AddEvent(updateEvent.ToJson());
        await Task.Delay(500);

        // Assert
        await AssertEventuallyAsync(async () =>
        {
            await using MySqlConnection connection = new MySqlConnection(ConnectionStringQueryRepoMySql);
            await connection.OpenAsync();

            string query = "SELECT COUNT(*) FROM Products WHERE product_id = @productId AND name = @name AND price = @price";
            await using MySqlCommand cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@productId", productId.ToString());
            cmd.Parameters.AddWithValue("@name", updatedName);
            cmd.Parameters.AddWithValue("@price", updatedPrice);

            long count = (long)(await cmd.ExecuteScalarAsync())!;
            return count == 1;
        }, timeoutMs: 5000);

        await AssertEventuallyAsync(async () =>
        {
            Guid lastEventId = await _queryRepo.GetLastSuccessfulEventId();
            return lastEventId == updateEventId;
        }, timeoutMs: 5000);
    }

    [Test]
    public async Task HappyFlow_DELETE_Should_Sync_From_MongoDB_To_MySQL()
    {
        // Arrange
        Guid insertEventId = Guid.NewGuid();
        Guid productId = Guid.NewGuid();

        BsonDocument insertEvent = BsonEventBuilder.Create()
            .WithId(insertEventId)
            .WithOccurredAt(DateTime.UtcNow)
            .WithAggregateName("Products")
            .WithStatus("PENDING")
            .WithInsertPayload(new Dictionary<string, object>
            {
                { "product_id", productId.ToString() },
                { "name", "Product To Delete" },
                { "sku", "DELETE-TEST" },
                { "price", 25.00 },
                { "stock_level", 10 },
                { "is_active", false }
            })
            .Build();

        MongoUrl url = new(ConnectionStringCommandRepoMongo);
        MongoClient client = new MongoClient(url);
        IMongoDatabase database = client.GetDatabase(url.DatabaseName);
        IMongoCollection<BsonDocument> collection = database.GetCollection<BsonDocument>("events");
        await collection.InsertOneAsync(insertEvent);
        _projector.AddEvent(insertEvent.ToJson());
        await Task.Delay(500);

        await AssertEventuallyAsync(async () =>
        {
            Guid lastEventId = await _queryRepo.GetLastSuccessfulEventId();
            return lastEventId == insertEventId;
        }, timeoutMs: 5000);

        await using (MySqlConnection connection = new MySqlConnection(ConnectionStringQueryRepoMySql))
        {
            await connection.OpenAsync();
            string checkQuery = "SELECT COUNT(*) FROM Products WHERE product_id = @productId";
            await using MySqlCommand checkCmd = new MySqlCommand(checkQuery, connection);
            checkCmd.Parameters.AddWithValue("@productId", productId.ToString());
            long countBefore = (long)(await checkCmd.ExecuteScalarAsync())!;
            Assert.That(countBefore, Is.EqualTo(1), "Product should exist before deletion");
        }

        // Act
        Guid deleteEventId = Guid.NewGuid();

        BsonDocument deleteEvent = BsonEventBuilder.Create()
            .WithId(deleteEventId)
            .WithOccurredAt(DateTime.UtcNow)
            .WithAggregateName("Products")
            .WithStatus("PENDING")
            .WithDeletePayload(new Dictionary<string, object>
            {
                { "product_id", productId.ToString() }
            })
            .Build();

        await collection.InsertOneAsync(deleteEvent);
        _projector.AddEvent(deleteEvent.ToJson());
        await Task.Delay(500);

        // Assert
        await AssertEventuallyAsync(async () =>
        {
            await using MySqlConnection connection = new MySqlConnection(ConnectionStringQueryRepoMySql);
            await connection.OpenAsync();

            string query = "SELECT COUNT(*) FROM Products WHERE product_id = @productId";
            await using MySqlCommand cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@productId", productId.ToString());

            long count = (long)(await cmd.ExecuteScalarAsync())!;
            return count == 0;
        }, timeoutMs: 5000);

        await AssertEventuallyAsync(async () =>
        {
            Guid lastEventId = await _queryRepo.GetLastSuccessfulEventId();
            return lastEventId == deleteEventId;
        }, timeoutMs: 5000);
    }

    [Test]
    public async Task LastEventId_Should_Update_In_MySQL_After_Each_Projection()
    {
        // Arrange
        Guid initialLastEventId = await _queryRepo.GetLastSuccessfulEventId();
        Assert.That(initialLastEventId, Is.EqualTo(Guid.Empty), "Last event ID should be empty initially");

        // Act 1
        Guid firstEventId = Guid.NewGuid();
        Guid firstProductId = Guid.NewGuid();

        BsonDocument firstEvent = BsonEventBuilder.Create()
            .WithId(firstEventId)
            .WithOccurredAt(DateTime.UtcNow)
            .WithAggregateName("Products")
            .WithStatus("PENDING")
            .WithInsertPayload(new Dictionary<string, object>
            {
                { "product_id", firstProductId.ToString() },
                { "name", "First Product" },
                { "sku", "TRACK-001" },
                { "price", 10.00 },
                { "stock_level", 5 },
                { "is_active", true }
            })
            .Build();

        MongoUrl url = new(ConnectionStringCommandRepoMongo);
        MongoClient client = new MongoClient(url);
        IMongoDatabase database = client.GetDatabase(url.DatabaseName);
        IMongoCollection<BsonDocument> collection = database.GetCollection<BsonDocument>("events");
        await collection.InsertOneAsync(firstEvent);
        _projector.AddEvent(firstEvent.ToJson());
        await Task.Delay(500);

        // Assert 1
        await AssertEventuallyAsync(async () =>
        {
            Guid lastEventId = await _queryRepo.GetLastSuccessfulEventId();
            return lastEventId == firstEventId;
        }, timeoutMs: 5000);

        await using (MySqlConnection connection = new MySqlConnection(ConnectionStringQueryRepoMySql))
        {
            await connection.OpenAsync();
            string query = "SELECT last_event_id FROM last_info WHERE id = 1";
            await using MySqlCommand cmd = new MySqlCommand(query, connection);
            string? storedEventId = (await cmd.ExecuteScalarAsync())?.ToString();
            Assert.That(storedEventId, Is.EqualTo(firstEventId.ToString()), "First event ID should be stored in last_info");
        }

        // Act 2
        await Task.Delay(100);
        Guid secondEventId = Guid.NewGuid();
        Guid secondProductId = Guid.NewGuid();

        BsonDocument secondEvent = BsonEventBuilder.Create()
            .WithId(secondEventId)
            .WithOccurredAt(DateTime.UtcNow)
            .WithAggregateName("Products")
            .WithStatus("PENDING")
            .WithInsertPayload(new Dictionary<string, object>
            {
                { "product_id", secondProductId.ToString() },
                { "name", "Second Product" },
                { "sku", "TRACK-002" },
                { "price", 20.00 },
                { "stock_level", 15 },
                { "is_active", true }
            })
            .Build();

        await collection.InsertOneAsync(secondEvent);
        _projector.AddEvent(secondEvent.ToJson());
        await Task.Delay(500);

        // Assert 2
        await AssertEventuallyAsync(async () =>
        {
            Guid lastEventId = await _queryRepo.GetLastSuccessfulEventId();
            return lastEventId == secondEventId;
        }, timeoutMs: 5000);

        await using (MySqlConnection connection = new MySqlConnection(ConnectionStringQueryRepoMySql))
        {
            await connection.OpenAsync();
            string query = "SELECT last_event_id FROM last_info WHERE id = 1";
            await using MySqlCommand cmd = new MySqlCommand(query, connection);
            string? storedEventId = (await cmd.ExecuteScalarAsync())?.ToString();
            Assert.That(storedEventId, Is.EqualTo(secondEventId.ToString()), "Second event ID should replace first in last_info");
            Assert.That(storedEventId, Is.Not.EqualTo(firstEventId.ToString()), "Last event ID should have changed from first event");
        }

        // Assert 3
        await using (MySqlConnection connection = new MySqlConnection(ConnectionStringQueryRepoMySql))
        {
            await connection.OpenAsync();
            string query = "SELECT COUNT(*) FROM Products WHERE product_id IN (@firstProductId, @secondProductId)";
            await using MySqlCommand cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@firstProductId", firstProductId.ToString());
            cmd.Parameters.AddWithValue("@secondProductId", secondProductId.ToString());
            long count = (long)(await cmd.ExecuteScalarAsync())!;
            Assert.That(count, Is.EqualTo(2), "Both products should exist in MySQL");
        }
    }

    private async Task AssertEventuallyAsync(Func<Task<bool>> condition, int timeoutMs)
    {
        DateTime start = DateTime.UtcNow;
        while ((DateTime.UtcNow - start).TotalMilliseconds < timeoutMs)
        {
            if (await condition()) return;
            await Task.Delay(100);
        }

        Assert.Fail($"Condition was not met within {timeoutMs}ms");
    }
}
