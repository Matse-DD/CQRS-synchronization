using MongoDB.Bson;
using MongoDB.Driver;
using MySql.Data.MySqlClient;

namespace IntegrationTests.Replay;

public class TestReplay
{
    private const string ConnectionStringToStartRepoMySql = "Server=localhost;Port=13306;User=root;Password=;";
    private const string ConnectionStringQueryRepoMySql = "Server=localhost;Port=13306;Database=cqrs_read;User=root;Password=;";
    private const string ConnectionStringCommandRepoMongo = "mongodb://localhost:27017/?connect=direct&replicaSet=rs0";

    [OneTimeSetUp]
    public async Task SetUpDatabases()
    {
        await using MySqlConnection connectionMySql = new MySqlConnection(ConnectionStringToStartRepoMySql);
        await connectionMySql.OpenAsync();

        string queryToStart = "CREATE DATABASE IF NOT EXISTS cqrs_read; USE cqrs_read; CREATE TABLE IF NOT EXISTS Products (product_id CHAR(36) PRIMARY KEY, name VARCHAR(255) NOT NULL, price DECIMAL(10,2) NOT NULL stock_level INT NOT NULL, is_active BOOLEAN NOT NULL); CREATE TABLE IF NOT EXISTS last_info (id INT AUTO_INCREMENT PRIMARY KEY, last_event_id CHAR(36));";
        await using MySqlCommand cmdGetLastEventId = new MySqlCommand(queryToStart, connectionMySql);
        await cmdGetLastEventId.ExecuteNonQueryAsync();
    }

    [TearDown]
    public async Task CleanUp()
    {
        await using MySqlConnection connectionMySql = new MySqlConnection(ConnectionStringQueryRepoMySql);
        await connectionMySql.OpenAsync();
        const string cleanupSql = "TRUNCATE TABLE Products; UPDATE last_info SET last_event_id = NULL WHERE id = 1;";
        await using MySqlCommand cmdCleanup = new MySqlCommand(cleanupSql, connectionMySql);
        await cmdCleanup.ExecuteNonQueryAsync();
        
        MongoClient client = new MongoClient(ConnectionStringCommandRepoMongo);
        IMongoDatabase? database = client.GetDatabase("users");
        IMongoCollection<BsonDocument>? collection = database.GetCollection<BsonDocument>("events");
        
        await collection.DeleteManyAsync(Builders<BsonDocument>.Filter.Empty);
    }
}