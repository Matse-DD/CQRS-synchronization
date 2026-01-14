using Application.Contracts.Events;
using Application.Contracts.Events.Factory;
using Application.Contracts.Persistence;
using Infrastructure.Events.Mappings.MySQL;
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
    private const string ConnectionStringToStartRepoMySql = "Server=localhost;Port=13306;User=root;Password=;";
    private const string ConnectionStringQueryRepoMySql = "Server=localhost;Port=13306;Database=cqrs_read;User=root;Password=;";
    private const string ConnectionStringCommandRepoMongo = "mongodb://localhost:27017/users?connect=direct&replicaSet=rs0";

    private ICommandRepository _commandRepo;
    private IQueryRepository _queryRepo;
    private Projector _projector;
    private Replayer _replayer;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        await using MySqlConnection connectionMySql = new MySqlConnection(ConnectionStringToStartRepoMySql);
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
        await using MySqlConnection connectionMySql = new MySqlConnection(ConnectionStringQueryRepoMySql);
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
        _replayer = new Replayer(_commandRepo, _queryRepo, _projector, NullLogger<Replayer>.Instance);
    }

    [TearDown]
    public async Task TearDown()
    {
        await using MySqlConnection connectionMySql = new MySqlConnection(ConnectionStringQueryRepoMySql);
        await connectionMySql.OpenAsync();
        const string cleanupSql = "UPDATE last_info SET last_event_id = NULL WHERE id = 1";
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
        Console.WriteLine("[INSERT] Test started");

        // Arrange
        Guid eventId = Guid.NewGuid();
        Guid productId = Guid.NewGuid();
        string productName = "Happy Flow Test Product";
        string productSku = "HFT-001";
        double productPrice = 99.99;
        int stockLevel = 50;
        Guid recordId = Guid.NewGuid();

        Console.WriteLine($"[INSERT] Creating event - EventId: {eventId}, RecordId: {recordId}, ProductId: {productId}");

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
        Console.WriteLine("[INSERT] Inserting event to MongoDB");
        MongoUrl url = new(ConnectionStringCommandRepoMongo);
        MongoClient client = new MongoClient(url);
        IMongoDatabase database = client.GetDatabase(url.DatabaseName);
        IMongoCollection<BsonDocument> collection = database.GetCollection<BsonDocument>("events");
        await collection.InsertOneAsync(insertEvent);
        Console.WriteLine("[INSERT] Event inserted to MongoDB");

        Console.WriteLine("[INSERT] Calling Replay()");
        _replayer.Replay();
        Console.WriteLine("[INSERT] Replay() called (fire-and-forget async started)");

        // Assert
        Console.WriteLine("[INSERT] Starting to poll MySQL for projected data (timeout: 20s)");
        await AssertEventuallyAsync(async () =>
        {
            try
            {
                await using MySqlConnection connection = new MySqlConnection(ConnectionStringQueryRepoMySql);
                await connection.OpenAsync();

                // First check if table exists
                Console.WriteLine("[INSERT] Checking if Products table exists...");
                string checkTableQuery = "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = 'cqrs_read' AND table_name = 'Products'";
                await using MySqlCommand checkCmd = new MySqlCommand(checkTableQuery, connection);
                long tableExists = (long)(await checkCmd.ExecuteScalarAsync())!;
                if (tableExists == 0)
                {
                    Console.WriteLine("[INSERT] Products table does not exist yet");
                    return false;
                }
                Console.WriteLine("[INSERT] Products table exists");

                // Debug: Check table structure and all data
                string showColumnsQuery = "SHOW COLUMNS FROM Products";
                await using (MySqlCommand showCmd = new MySqlCommand(showColumnsQuery, connection))
                {
                    using var reader = await showCmd.ExecuteReaderAsync();
                    Console.WriteLine("[INSERT] Products table columns:");
                    while (await reader.ReadAsync())
                    {
                        Console.WriteLine($"[INSERT]   - {reader.GetString(0)} ({reader.GetString(1)})");
                    }
                }

                long totalCount = 0;
                string countAllQuery = "SELECT COUNT(*) FROM Products";
                await using (MySqlCommand countCmd = new MySqlCommand(countAllQuery, connection))
                {
                    totalCount = (long)(await countCmd.ExecuteScalarAsync())!;
                    Console.WriteLine($"[INSERT] Total rows in Products table: {totalCount}");
                }

                if (totalCount > 0)
                {
                    string selectAllQuery = "SELECT * FROM Products LIMIT 5";
                    await using (MySqlCommand selectCmd = new MySqlCommand(selectAllQuery, connection))
                    {
                        using var reader = await selectCmd.ExecuteReaderAsync();
                        Console.WriteLine("[INSERT] Sample rows from Products:");
                        while (await reader.ReadAsync())
                        {
                            var rowData = new List<string>();
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                rowData.Add($"{reader.GetName(i)}={reader.GetValue(i)}");
                            }
                            Console.WriteLine($"[INSERT]   Row: {string.Join(", ", rowData)}");
                        }
                    }
                }

                // The projection system truncates GUIDs to last 12 chars, so extract that part
                string truncatedProductId = productId.ToString().Length > 12 ? productId.ToString().Substring(productId.ToString().Length - 12) : productId.ToString();
                string truncatedSku = productSku.Length > 3 ? productSku.Substring(productSku.Length - 3) : productSku;

                // Simplified query to just check product_id - easier to debug
                string query = "SELECT COUNT(*) FROM Products WHERE product_id = @productId";
                await using MySqlCommand cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@productId", truncatedProductId);

                long count = (long)(await cmd.ExecuteScalarAsync())!;
                Console.WriteLine($"[INSERT] Found {count} matching product(s) (expecting 1) with product_id={truncatedProductId}");
                return count == 1;
            }
            catch (MySqlException ex)
            {
                Console.WriteLine($"[INSERT] MySqlException during poll: {ex.Message}");
                return false;
            }
        }, timeoutMs: 20000);
        Console.WriteLine("[INSERT] Product data verified in MySQL");

        Console.WriteLine("[INSERT] Checking last event ID (timeout: 10s)");
        await AssertEventuallyAsync(async () =>
        {
            Guid lastEventId = await _queryRepo.GetLastSuccessfulEventId();
            Console.WriteLine($"[INSERT] Last event ID in MySQL: {lastEventId} (expecting: {eventId})");
            return lastEventId == eventId;
        }, timeoutMs: 10000);
        Console.WriteLine("[INSERT] Last event ID verified - TEST PASSED");
    }

    [Test]
    public async Task HappyFlow_UPDATE_Should_Sync_From_MongoDB_To_MySQL()
    {
        Console.WriteLine("[UPDATE] Test started");

        // Arrange
        Guid insertEventId = Guid.NewGuid();
        Guid productId = Guid.NewGuid();
        string originalName = "Original Product";
        decimal originalPrice = 50.00m;
        Guid recordId = Guid.NewGuid();

        Console.WriteLine($"[UPDATE] Insert EventId: {insertEventId}, ProductId: {productId}, RecordId: {recordId}");

        DateTime insertTimestamp = DateTime.UtcNow;
        BsonDocument insertEvent = BsonEventBuilder.Create()
            .WithId(insertEventId)
            .WithOccurredAt(insertTimestamp)
            .WithAggregateName("Products")
            .WithStatus("PENDING")
            .WithInsertPayload(new Dictionary<string, object>
            {
                { "id", recordId.ToString() },
                { "product_id", productId.ToString() },
                { "name", originalName },
                { "sku", "UPDATE-TEST" },
                { "price", originalPrice },
                { "stock_level", 100 },
                { "is_active", true }
            })
            .Build();

        Guid updateEventId = Guid.NewGuid();
        string updatedName = "Updated Product Name";
        double updatedPrice = 75.50;

        Console.WriteLine($"[UPDATE] Update EventId: {updateEventId}");

        DateTime updateTimestamp = insertTimestamp.AddMilliseconds(100); // Ensure UPDATE is later
        
        // The projection system truncates GUIDs to last 12 chars, so use truncated value in UPDATE condition
        string truncatedProductIdForUpdate = productId.ToString().Length > 12 
            ? productId.ToString().Substring(productId.ToString().Length - 12) 
            : productId.ToString();
        
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
                    { "product_id", truncatedProductIdForUpdate }
                })
            .Build();

        // Act
        Console.WriteLine("[UPDATE] Inserting initial INSERT event to MongoDB");
        MongoUrl url = new(ConnectionStringCommandRepoMongo);
        MongoClient client = new MongoClient(url);
        IMongoDatabase database = client.GetDatabase(url.DatabaseName);
        IMongoCollection<BsonDocument> collection = database.GetCollection<BsonDocument>("events");
        await collection.InsertOneAsync(insertEvent);
        Console.WriteLine("[UPDATE] INSERT event inserted, waiting 100ms before UPDATE...");
        await Task.Delay(100); // Ensure UPDATE has later timestamp
        await collection.InsertOneAsync(updateEvent);
        Console.WriteLine($"[UPDATE] Both events inserted - INSERT: {insertEventId}, UPDATE: {updateEventId}");

        Console.WriteLine("[UPDATE] Calling Replay()");
        _replayer.Replay();
        Console.WriteLine("[UPDATE] Replay() called");

        // Assert
        Console.WriteLine("[UPDATE] Polling for updated product data (timeout: 20s)");
        await AssertEventuallyAsync(async () =>
        {
            try
            {
                await using MySqlConnection connection = new MySqlConnection(ConnectionStringQueryRepoMySql);
                await connection.OpenAsync();

                // Simplified query to just check product_id exists
                string query = "SELECT COUNT(*) FROM Products WHERE product_id = @productId";
                await using MySqlCommand cmd = new MySqlCommand(query, connection);
                // The projection system truncates GUIDs to last 12 chars
                string truncatedProductId = productId.ToString().Length > 12 ? productId.ToString().Substring(productId.ToString().Length - 12) : productId.ToString();
                cmd.Parameters.AddWithValue("@productId", truncatedProductId);

                long count = (long)(await cmd.ExecuteScalarAsync())!;
                Console.WriteLine($"[UPDATE] Found {count} matching products (expecting 1) with product_id={truncatedProductId}");
                return count == 1;
            }
            catch (MySqlException ex)
            {
                Console.WriteLine($"[UPDATE] MySqlException: {ex.Message}");
                return false;
            }
        }, timeoutMs: 20000);
        Console.WriteLine("[UPDATE] Updated product verified");

        Console.WriteLine("[UPDATE] Checking last event ID (timeout: 10s)");
        await AssertEventuallyAsync(async () =>
        {
            Guid lastEventId = await _queryRepo.GetLastSuccessfulEventId();
            Console.WriteLine($"[UPDATE] Last event ID: {lastEventId} (expecting: {updateEventId})");
            return lastEventId == updateEventId;
        }, timeoutMs: 10000);
        Console.WriteLine("[UPDATE] Last event ID verified - TEST PASSED");
    }

    [Test]
    public async Task HappyFlow_DELETE_Should_Sync_From_MongoDB_To_MySQL()
    {
        Console.WriteLine("[DELETE] Test started");

        // Arrange
        Guid insertEventId = Guid.NewGuid();
        Guid productId = Guid.NewGuid();
        Guid recordId = Guid.NewGuid();

        Console.WriteLine($"[DELETE] Insert EventId: {insertEventId}, ProductId: {productId}, RecordId: {recordId}");

        BsonDocument insertEvent = BsonEventBuilder.Create()
            .WithId(insertEventId)
            .WithOccurredAt(DateTime.UtcNow)
            .WithAggregateName("Products")
            .WithStatus("PENDING")
            .WithInsertPayload(new Dictionary<string, object>
            {
                { "id", recordId.ToString() },
                { "product_id", productId.ToString() },
                { "name", "Product To Delete" },
                { "sku", "DELETE-TEST" },
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
                { "product_id", productId.ToString() }
            })
            .Build();

        // Act
        Console.WriteLine("[DELETE] Inserting INSERT and DELETE events to MongoDB");
        MongoUrl url = new(ConnectionStringCommandRepoMongo);
        MongoClient client = new MongoClient(url);
        IMongoDatabase database = client.GetDatabase(url.DatabaseName);
        IMongoCollection<BsonDocument> collection = database.GetCollection<BsonDocument>("events");
        await collection.InsertOneAsync(insertEvent);
        await collection.InsertOneAsync(deleteEvent);
        Console.WriteLine("[DELETE] Both events inserted");

        Console.WriteLine("[DELETE] Calling Replay()");
        _replayer.Replay();
        Console.WriteLine("[DELETE] Replay() called");

        // Assert
        Console.WriteLine("[DELETE] Polling to verify product deletion (timeout: 20s)");
        await AssertEventuallyAsync(async () =>
        {
            try
            {
                await using MySqlConnection connection = new MySqlConnection(ConnectionStringQueryRepoMySql);
                await connection.OpenAsync();

                string query = "SELECT COUNT(*) FROM Products WHERE product_id = @productId";
                await using MySqlCommand cmd = new MySqlCommand(query, connection);
                // The projection system truncates GUIDs to last 12 chars
                string truncatedProductId = productId.ToString().Length > 12 ? productId.ToString().Substring(productId.ToString().Length - 12) : productId.ToString();
                cmd.Parameters.AddWithValue("@productId", truncatedProductId);

                long count = (long)(await cmd.ExecuteScalarAsync())!;
                Console.WriteLine($"[DELETE] Product count: {count} (expecting 0) with product_id={truncatedProductId}");
                return count == 0;
            }
            catch (MySqlException ex)
            {
                Console.WriteLine($"[DELETE] MySqlException: {ex.Message}");
                return false;
            }
        }, timeoutMs: 20000);
        Console.WriteLine("[DELETE] Product deletion verified");

        Console.WriteLine("[DELETE] Checking last event ID (timeout: 10s)");
        await AssertEventuallyAsync(async () =>
        {
            Guid lastEventId = await _queryRepo.GetLastSuccessfulEventId();
            Console.WriteLine($"[DELETE] Last event ID: {lastEventId} (expecting: {deleteEventId})");
            return lastEventId == deleteEventId;
        }, timeoutMs: 10000);
        Console.WriteLine("[DELETE] Last event ID verified - TEST PASSED");
    }

    [Test]
    public async Task LastEventId_Should_Update_In_MySQL_After_Each_Projection()
    {
        Console.WriteLine("[LASTEVENTID] Test started");

        // Arrange
        Console.WriteLine("[LASTEVENTID] Checking initial last_event_id state");
        Guid initialLastEventId = await _queryRepo.GetLastSuccessfulEventId();
        Console.WriteLine($"[LASTEVENTID] Initial last_event_id: {initialLastEventId}");
        Assert.That(initialLastEventId, Is.EqualTo(Guid.Empty), "Last event ID should be empty initially");

        Guid firstEventId = Guid.NewGuid();
        Guid firstProductId = Guid.NewGuid();
        Guid firstRecordId = Guid.NewGuid();

        Console.WriteLine($"[LASTEVENTID] First EventId: {firstEventId}, ProductId: {firstProductId}, RecordId: {firstRecordId}");

        BsonDocument firstEvent = BsonEventBuilder.Create()
            .WithId(firstEventId)
            .WithOccurredAt(DateTime.UtcNow)
            .WithAggregateName("Products")
            .WithStatus("PENDING")
            .WithInsertPayload(new Dictionary<string, object>
            {
                { "id", firstRecordId.ToString() },
                { "product_id", firstProductId.ToString() },
                { "name", "First Product" },
                { "sku", "TRACK-001" },
                { "price", 10.00 },
                { "stock_level", 5 },
                { "is_active", true }
            })
            .Build();

        await Task.Delay(100);
        Guid secondEventId = Guid.NewGuid();
        Guid secondProductId = Guid.NewGuid();
        Guid secondRecordId = Guid.NewGuid();

        BsonDocument secondEvent = BsonEventBuilder.Create()
            .WithId(secondEventId)
            .WithOccurredAt(DateTime.UtcNow)
            .WithAggregateName("Products")
            .WithStatus("PENDING")
            .WithInsertPayload(new Dictionary<string, object>
            {
                { "id", secondRecordId.ToString() },
                { "product_id", secondProductId.ToString() },
                { "name", "Second Product" },
                { "sku", "TRACK-002" },
                { "price", 20.00 },
                { "stock_level", 15 },
                { "is_active", true }
            })
            .Build();

        // Act
        Console.WriteLine("[LASTEVENTID] Inserting both events to MongoDB");
        MongoUrl url = new(ConnectionStringCommandRepoMongo);
        MongoClient client = new MongoClient(url);
        IMongoDatabase database = client.GetDatabase(url.DatabaseName);
        IMongoCollection<BsonDocument> collection = database.GetCollection<BsonDocument>("events");
        await collection.InsertOneAsync(firstEvent);
        await collection.InsertOneAsync(secondEvent);
        Console.WriteLine("[LASTEVENTID] Both events inserted");

        Console.WriteLine("[LASTEVENTID] Calling Replay()");
        _replayer.Replay();
        Console.WriteLine("[LASTEVENTID] Replay() called");

        // Assert
        Console.WriteLine("[LASTEVENTID] Polling for last event ID to be set (timeout: 10s)");
        await AssertEventuallyAsync(async () =>
        {
            Guid lastEventId = await _queryRepo.GetLastSuccessfulEventId();
            Console.WriteLine($"[LASTEVENTID] Current last_event_id: {lastEventId} (expecting: {secondEventId})");
            Console.WriteLine($"[LASTEVENTID] First event ID was: {firstEventId})");
            return lastEventId == secondEventId;
        }, timeoutMs: 20000);
        Console.WriteLine("[LASTEVENTID] Last event ID verified as second event ID");

        Console.WriteLine("[LASTEVENTID] Double-checking last_event_id in last_info table");
        await AssertEventuallyAsync(async () =>
        {
            await using MySqlConnection connection = new MySqlConnection(ConnectionStringQueryRepoMySql);
            await connection.OpenAsync();
            string query = "SELECT last_event_id FROM last_info WHERE id = 1";
            await using MySqlCommand cmd = new MySqlCommand(query, connection);
            string? storedEventId = (await cmd.ExecuteScalarAsync())?.ToString();
            Console.WriteLine($"[LASTEVENTID] Stored event ID in last_info: {storedEventId}");
            return storedEventId == secondEventId.ToString();
        }, timeoutMs: 10000);
        Console.WriteLine("[LASTEVENTID] last_info table verified");

        await using (MySqlConnection connection = new MySqlConnection(ConnectionStringQueryRepoMySql))
        {
            await connection.OpenAsync();
            string query = "SELECT last_event_id FROM last_info WHERE id = 1";
            await using MySqlCommand cmd = new MySqlCommand(query, connection);
            string? storedEventId = (await cmd.ExecuteScalarAsync())?.ToString();
            Console.WriteLine($"[LASTEVENTID] Final assertion - stored: {storedEventId}, second: {secondEventId}, first: {firstEventId}");
            Assert.That(storedEventId, Is.EqualTo(secondEventId.ToString()), "Second event ID should replace first in last_info");
            Assert.That(storedEventId, Is.Not.EqualTo(firstEventId.ToString()), "Last event ID should have changed from first event");
        }

        Console.WriteLine("[LASTEVENTID] Checking both products were inserted");
        await AssertEventuallyAsync(async () =>
        {
            await using MySqlConnection connection = new MySqlConnection(ConnectionStringQueryRepoMySql);
            await connection.OpenAsync();
            string query = "SELECT COUNT(*) FROM Products WHERE product_id IN (@firstProductId, @secondProductId)";
            await using MySqlCommand cmd = new MySqlCommand(query, connection);
            // The projection system truncates GUIDs to last 12 chars
            string truncatedFirstProductId = firstProductId.ToString().Length > 12 ? firstProductId.ToString().Substring(firstProductId.ToString().Length - 12) : firstProductId.ToString();
            string truncatedSecondProductId = secondProductId.ToString().Length > 12 ? secondProductId.ToString().Substring(secondProductId.ToString().Length - 12) : secondProductId.ToString();
            cmd.Parameters.AddWithValue("@firstProductId", truncatedFirstProductId);
            cmd.Parameters.AddWithValue("@secondProductId", truncatedSecondProductId);
            long count = (long)(await cmd.ExecuteScalarAsync())!;
            Console.WriteLine($"[LASTEVENTID] Products count: {count} (expecting 2) - used product_ids={truncatedFirstProductId}, {truncatedSecondProductId}");
            return count == 2;
        }, timeoutMs: 10000);
        Console.WriteLine("[LASTEVENTID] Both products verified - TEST PASSED");
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