using Application.Contracts.Events;
using Application.Contracts.Events.Factory;
using Application.Contracts.Persistence;
using Infrastructure.Events.Mappings.MySQL;
using Infrastructure.Observer;
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
    private MongoDbObserver _observer;
    private CancellationTokenSource _cancellationTokenSource;

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
        const string cleanupSql = "DROP TABLE IF EXISTS Products; DELETE FROM last_info WHERE collection_name = 'events'";
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
        _observer = new MongoDbObserver(ConnectionStringCommandRepoMongo, NullLogger<MongoDbObserver>.Instance);

        _cancellationTokenSource = new CancellationTokenSource();
        _ = _observer.StartListening(_projector.AddEvent, _cancellationTokenSource.Token);
        
        await Task.Delay(500);
    }
    
    [TearDown]
    public async Task TearDown()
    {
        await _cancellationTokenSource.CancelAsync();
        _cancellationTokenSource.Dispose();

        await using MySqlConnection connectionMySql = new MySqlConnection(ConnectionStringQueryRepoMySql);
        await connectionMySql.OpenAsync();
        const string cleanupSql = "DROP TABLE IF EXISTS Products; DELETE FROM last_info WHERE collection_name = 'events'";
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

}