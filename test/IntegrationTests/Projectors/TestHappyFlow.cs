using Application.Contracts.Events;
using Application.Contracts.Events.Factory;
using Application.Contracts.Persistence;
using Infrastructure.Events.Mappings.MySQL;
using Infrastructure.Events.Mappings.Shared;
using Infrastructure.Persistence.CommandRepository;
using Infrastructure.Persistence.QueryRepository;
using Infrastructure.Projectors;
using Infrastructure.Replay;
using IntegrationTests.Helpers;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;
using MongoDB.Driver;
using MySql.Data.MySqlClient;

namespace IntegrationTests.Projectors;

public class TestHappyFlow
{
    private ICommandRepository _commandRepo;
    private IQueryRepository _queryRepo;
    private Projector _projector;
    private Replayer _replayer;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        await using MySqlConnection connectionMySql = new MySqlConnection(TestConnectionStrings.MySqlSetup);
        await connectionMySql.OpenAsync();

        string queryToStart = "CREATE DATABASE IF NOT EXISTS cqrs_read; USE cqrs_read;";
        await using MySqlCommand cmdCreateDb = new MySqlCommand(queryToStart, connectionMySql);
        await cmdCreateDb.ExecuteNonQueryAsync();

        string createTableSql = "CREATE TABLE IF NOT EXISTS last_info (id INT AUTO_INCREMENT PRIMARY KEY, last_event_id CHAR(36));";
        await using MySqlCommand cmdCreateTable = new MySqlCommand(createTableSql, connectionMySql);
        await cmdCreateTable.ExecuteNonQueryAsync();

        string insertInitialSql = "INSERT IGNORE INTO last_info (id, last_event_id) VALUES (1, NULL);";
        await using MySqlCommand cmdInsert = new MySqlCommand(insertInitialSql, connectionMySql);
        await cmdInsert.ExecuteNonQueryAsync();
    }

    [SetUp]
    public async Task SetUp()
    {
        await using MySqlConnection connectionMySql = new MySqlConnection(TestConnectionStrings.MySqlQuery);
        await connectionMySql.OpenAsync();
        const string dropProductsSql = "DROP TABLE IF EXISTS Products";
        await using (MySqlCommand dropProducts = new MySqlCommand(dropProductsSql, connectionMySql))
        {
            await dropProducts.ExecuteNonQueryAsync();
        }

        const string resetLastInfoSql = "DELETE FROM last_info;";
        await using (MySqlCommand resetLastInfo = new MySqlCommand(resetLastInfoSql, connectionMySql))
        {
            await resetLastInfo.ExecuteNonQueryAsync();
        }

        const string insertLastInfoSql = "INSERT INTO last_info (id, last_event_id) VALUES (1, NULL)";
        await using (MySqlCommand insertLastInfo = new MySqlCommand(insertLastInfoSql, connectionMySql))
        {
            await insertLastInfo.ExecuteNonQueryAsync();
        }

        MongoUrl mongoUrl = new(TestConnectionStrings.MongoDbCommand);
        MongoClient client = new MongoClient(mongoUrl);
        IMongoDatabase? database = client.GetDatabase(mongoUrl.DatabaseName);
        IMongoCollection<BsonDocument>? collection = database.GetCollection<BsonDocument>("events");
        await collection.DeleteManyAsync(Builders<BsonDocument>.Filter.Empty);

        _commandRepo = new MongoDbCommandRepository(TestConnectionStrings.MongoDbCommand, NullLogger<MongoDbCommandRepository>.Instance);
        _queryRepo = new MySqlQueryRepository(TestConnectionStrings.MySqlQuery, NullLogger<MySqlQueryRepository>.Instance);
        IEventFactory eventFactory = new MySqlEventFactory();
        ISchemaBuilder schemaBuilder = new MySqlSchemaBuilder();

        _projector = new Projector(_commandRepo, _queryRepo, eventFactory, NullLogger<Projector>.Instance, schemaBuilder);
        _replayer = new Replayer(_commandRepo, _queryRepo, _projector, NullLogger<Replayer>.Instance);
    }

    [TearDown]
    public async Task TearDown()
    {
        await using MySqlConnection connectionMySql = new MySqlConnection(TestConnectionStrings.MySqlQuery);
        await connectionMySql.OpenAsync();
        const string cleanupSql = "UPDATE last_info SET last_event_id = NULL WHERE id = 1";
        await using MySqlCommand cmd = new MySqlCommand(cleanupSql, connectionMySql);
        await cmd.ExecuteNonQueryAsync();

        MongoUrl mongoUrl = new(TestConnectionStrings.MongoDbCommand);
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
        Guid recordId = Guid.NewGuid();

        BsonDocument insertEvent = BsonEventBuilder.Create()
            .WithId(eventId)
            .WithOccurredAt(DateTime.UtcNow)
            .WithAggregateName("Products")
            .WithStatus("PENDING")
            .WithInsertPayload(new Dictionary<string, object>
            {
                { "id", recordId.ToString() },
                { "product_id", productId.ToString() },
                { "name", productName },
                { "sku", productSku },
                { "price", productPrice },
                { "stock_level", stockLevel },
                { "is_active", true }
            })
            .Build();

        // Act
        MongoUrl url = new(TestConnectionStrings.MongoDbCommand);
        MongoClient client = new MongoClient(url);
        IMongoDatabase database = client.GetDatabase(url.DatabaseName);
        IMongoCollection<BsonDocument> collection = database.GetCollection<BsonDocument>("events");
        await collection.InsertOneAsync(insertEvent);

        _replayer.Replay();

        // Assert
        await AssertEventuallyAsync(async () =>
        {
            try
            {
                await using MySqlConnection connection = new MySqlConnection(TestConnectionStrings.MySqlQuery);
                await connection.OpenAsync();

                string checkTableQuery = "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = 'cqrs_read' AND table_name = 'Products'";
                await using MySqlCommand checkCmd = new MySqlCommand(checkTableQuery, connection);
                long tableExists = (long)(await checkCmd.ExecuteScalarAsync())!;
                if (tableExists == 0)
                {
                    return false;
                }

                string truncatedProductId = productId.ToString().Length > 12 ? productId.ToString().Substring(productId.ToString().Length - 12) : productId.ToString();

                string query = "SELECT COUNT(*) FROM Products WHERE product_id = @productId";
                await using MySqlCommand cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@productId", truncatedProductId);

                long count = (long)(await cmd.ExecuteScalarAsync())!;
                return count == 1;
            }
            catch (MySqlException)
            {
                return false;
            }
        }, timeoutMs: 20000);

        await AssertEventuallyAsync(async () =>
        {
            Guid lastEventId = await _queryRepo.GetLastSuccessfulEventId();
            return lastEventId == eventId;
        }, timeoutMs: 10000);
    }

    [Test]
    public async Task HappyFlow_UPDATE_Should_Sync_From_MongoDB_To_MySQL()
    {
        // Arrange
        Guid insertEventId = Guid.NewGuid();
        string productId = $"'{Guid.NewGuid()}'";
        string originalName = "'Original Product'";
        decimal originalPrice = 50.00m;
        string recordId = $"'{Guid.NewGuid()}'";

        DateTime insertTimestamp = DateTime.UtcNow;
        BsonDocument insertEvent = BsonEventBuilder.Create()
            .WithId(insertEventId)
            .WithOccurredAt(insertTimestamp)
            .WithAggregateName("Products")
            .WithStatus("PENDING")
            .WithInsertPayload(new Dictionary<string, object>
            {
                { "id", recordId },
                { "product_id", productId },
                { "name", originalName },
                { "sku", "'UPDATE-TEST'" },
                { "price", originalPrice },
                { "stock_level", 100 },
                { "is_active", true }
            })
            .Build();

        Guid updateEventId = Guid.NewGuid();
        string updatedName = "'Updated Product Name'";
        double updatedPrice = 75.50;

        DateTime updateTimestamp = insertTimestamp.AddMilliseconds(100);

        BsonDocument updateEvent = BsonEventBuilder.Create()
            .WithId(updateEventId)
            .WithOccurredAt(updateTimestamp)
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
                    { "product_id", productId }
                })
            .Build();

        // Act
        MongoUrl url = new(TestConnectionStrings.MongoDbCommand);
        MongoClient client = new MongoClient(url);
        IMongoDatabase database = client.GetDatabase(url.DatabaseName);
        IMongoCollection<BsonDocument> collection = database.GetCollection<BsonDocument>("events");
        await collection.InsertOneAsync(insertEvent);
        await Task.Delay(100);
        await collection.InsertOneAsync(updateEvent);

        _replayer.Replay();

        // Assert
        await AssertEventuallyAsync(async () =>
        {
            try
            {
                await using MySqlConnection connection = new MySqlConnection(TestConnectionStrings.MySqlQuery);
                await connection.OpenAsync();

                string query = "SELECT COUNT(*) FROM Products WHERE product_id = @productId";
                await using MySqlCommand cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@productId", productId.ExtractValue());

                long count = (long)(await cmd.ExecuteScalarAsync())!;
                return count == 1;
            }
            catch (MySqlException)
            {
                return false;
            }
        }, timeoutMs: 20000);
    }

    [Test]
    public async Task HappyFlow_DELETE_Should_Sync_From_MongoDB_To_MySQL()
    {
        // Arrange
        Guid insertEventId = Guid.NewGuid();
        string productId = $"'{Guid.NewGuid()}'";
        string recordId = $"'{Guid.NewGuid()}'";

        BsonDocument insertEvent = BsonEventBuilder.Create()
            .WithId(insertEventId)
            .WithOccurredAt(DateTime.UtcNow)
            .WithAggregateName("Products")
            .WithStatus("PENDING")
            .WithInsertPayload(new Dictionary<string, object>
            {
                { "id", recordId },
                { "product_id", productId },
                { "name", "'Product To Delete'" },
                { "sku", "'DELETE-TEST'" },
                { "price", 25.00 },
                { "stock_level", 10 },
                { "is_active", false }
            })
            .Build();

        Guid deleteEventId = Guid.NewGuid();
        BsonDocument deleteEvent = BsonEventBuilder.Create()
            .WithId(deleteEventId)
            .WithOccurredAt(DateTime.UtcNow)
            .WithAggregateName("Products")
            .WithStatus("PENDING")
            .WithDeletePayload(new Dictionary<string, object>
            {
                { "product_id", productId }
            })
            .Build();

        // Act
        MongoUrl url = new(TestConnectionStrings.MongoDbCommand);
        MongoClient client = new MongoClient(url);
        IMongoDatabase database = client.GetDatabase(url.DatabaseName);
        IMongoCollection<BsonDocument> collection = database.GetCollection<BsonDocument>("events");
        await collection.InsertOneAsync(insertEvent);
        await collection.InsertOneAsync(deleteEvent);

        _replayer.Replay();

        // Assert
        await AssertEventuallyAsync(async () =>
        {
            try
            {
                await using MySqlConnection connection = new MySqlConnection(TestConnectionStrings.MySqlQuery);
                await connection.OpenAsync();

                string query = "SELECT COUNT(*) FROM Products WHERE product_id = @productId";
                await using MySqlCommand cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@productId", productId);

                long count = (long)(await cmd.ExecuteScalarAsync())!;
                return count == 0;
            }
            catch (MySqlException)
            {
                return false;
            }
        }, timeoutMs: 20000);

        await AssertEventuallyAsync(async () =>
        {
            Guid lastEventId = await _queryRepo.GetLastSuccessfulEventId();
            return lastEventId == deleteEventId;
        }, timeoutMs: 10000);
    }

    [Test]
    public async Task LastEventId_Should_Update_In_MySQL_After_Each_Projection()
    {
        // Arrange
        Guid initialLastEventId = await _queryRepo.GetLastSuccessfulEventId();
        Assert.That(initialLastEventId, Is.EqualTo(Guid.Empty), "Last event ID should be empty initially");

        Guid firstEventId = Guid.NewGuid();
        string firstProductId = $"'{Guid.NewGuid()}'";
        string firstRecordId = $"'{Guid.NewGuid()}'";

        BsonDocument firstEvent = BsonEventBuilder.Create()
            .WithId(firstEventId)
            .WithOccurredAt(DateTime.UtcNow)
            .WithAggregateName("Products")
            .WithStatus("PENDING")
            .WithInsertPayload(new Dictionary<string, object>
            {
                { "id", firstRecordId },
                { "product_id", firstProductId },
                { "name", "'First Product'" },
                { "sku", "'TRACK-001'" },
                { "price", 10.00 },
                { "stock_level", 5 },
                { "is_active", true }
            })
            .Build();

        await Task.Delay(100);
        Guid secondEventId = Guid.NewGuid();
        string secondProductId = $"'{Guid.NewGuid()}'";
        string secondRecordId = $"'{Guid.NewGuid()}'";

        BsonDocument secondEvent = BsonEventBuilder.Create()
            .WithId(secondEventId)
            .WithOccurredAt(DateTime.UtcNow)
            .WithAggregateName("Products")
            .WithStatus("PENDING")
            .WithInsertPayload(new Dictionary<string, object>
            {
                { "id", secondRecordId },
                { "product_id", secondProductId },
                { "name", "'Second Product'" },
                { "sku", "'TRACK-002'" },
                { "price", 20.00 },
                { "stock_level", 15 },
                { "is_active", true }
            })
            .Build();

        // Act
        MongoUrl url = new(TestConnectionStrings.MongoDbCommand);
        MongoClient client = new MongoClient(url);
        IMongoDatabase database = client.GetDatabase(url.DatabaseName);
        IMongoCollection<BsonDocument> collection = database.GetCollection<BsonDocument>("events");
        await collection.InsertOneAsync(firstEvent);
        await collection.InsertOneAsync(secondEvent);

        _replayer.Replay();

        // Assert
        await AssertEventuallyAsync(async () =>
        {
            Guid lastEventId = await _queryRepo.GetLastSuccessfulEventId();
            return lastEventId == secondEventId;
        }, timeoutMs: 20000);

        await AssertEventuallyAsync(async () =>
        {
            await using MySqlConnection connection = new MySqlConnection(TestConnectionStrings.MySqlQuery);
            await connection.OpenAsync();
            string query = "SELECT last_event_id FROM last_info WHERE id = 1";
            await using MySqlCommand cmd = new MySqlCommand(query, connection);
            string? storedEventId = (await cmd.ExecuteScalarAsync())?.ToString();
            return storedEventId == secondEventId.ToString();
        }, timeoutMs: 10000);

        await using (MySqlConnection connection = new MySqlConnection(TestConnectionStrings.MySqlQuery))
        {
            await connection.OpenAsync();
            string query = "SELECT last_event_id FROM last_info WHERE id = 1";
            await using MySqlCommand cmd = new MySqlCommand(query, connection);
            string? storedEventId = (await cmd.ExecuteScalarAsync())?.ToString();
            Assert.That(storedEventId, Is.EqualTo(secondEventId.ToString()), "Second event ID should replace first in last_info");
            Assert.That(storedEventId, Is.Not.EqualTo(firstEventId.ToString()), "Last event ID should have changed from first event");
        }

        await AssertEventuallyAsync(async () =>
        {
            await using MySqlConnection connection = new MySqlConnection(TestConnectionStrings.MySqlQuery);
            await connection.OpenAsync();
            string query = "SELECT COUNT(*) FROM Products WHERE product_id IN (@firstProductId, @secondProductId)";
            await using MySqlCommand cmd = new MySqlCommand(query, connection);

            cmd.Parameters.AddWithValue("@firstProductId", firstProductId.ExtractValue());
            cmd.Parameters.AddWithValue("@secondProductId", secondProductId.ExtractValue());
            long count = (long)(await cmd.ExecuteScalarAsync())!;
            return count == 2;
        }, timeoutMs: 10000);
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
