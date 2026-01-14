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

}