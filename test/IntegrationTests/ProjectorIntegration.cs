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

namespace IntegrationTests;

public class ProjectorIntegration
{
    private readonly string _connectionStringToStartRepoMySql = "Server=localhost;Port=13306;User=root;Password=;";
    private readonly string _connectionStringQueryRepoMySql = "Server=localhost;Port=13306;Database=cqrs_read;User=root;Password=;";
    private readonly string _connectionStringCommandRepoMongo = "mongodb://localhost:27017"; //?replicaSet=rs0

    [OneTimeSetUp]
    public async Task Set_DatabasesUp()
    {
        MySqlConnection connectionMySql = new MySqlConnection(_connectionStringToStartRepoMySql);
        connectionMySql.Open();

        string queryToStart = "CREATE DATABASE IF NOT EXISTS cqrs_command; USE cqrs_command; CREATE TABLE IF NOT EXISTS products (product_id CHAR(36) PRIMARY KEY, name VARCHAR(255) NOT NULL,sku VARCHAR(100) NOT NULL,price DECIMAL(10,2) NOT NULL,stock_level INT NOT NULL, is_active BOOLEAN NOT NULL);CREATE TABLE IF NOT EXISTS last_info (id INT AUTO_INCREMENT PRIMARY KEY, last_event_id CHAR(36) NOT NULL);-- Insert initial last_event_id with a new GUID\r\nINSERT INTO last_info (last_event_id) VALUES (UUID());";

        MySqlCommand cmdGetLastEventId = new MySqlCommand(queryToStart, connectionMySql);
        await cmdGetLastEventId.ExecuteReaderAsync();
    }

    [Test]
    public void Projector_Should_Project()
    {
        // Set infrastructure up
        ICommandRepository commandRepo = new MongoDbCommandRepository(_connectionStringCommandRepoMongo);
        IQueryRepository queryRepo = new MySqlQueryRepository(_connectionStringQueryRepoMySql);

        IEventFactory eventFactory = new MySqlEventFactory();

        Projector projector = new Projector(commandRepo, queryRepo, eventFactory);

        // Act
        ICollection<string> eventsAdded = AddEventToOutbox();

        // start listing for changes
        MongoDbObserver observer = new MongoDbObserver(_connectionStringCommandRepoMongo);
        observer.StartListening(projector.ProjectEvent);
        Recovery recover = new Recovery(commandRepo, queryRepo, projector);

        // Assert
        Task<Guid> eventId = queryRepo.GetLastSuccessfulEventId();

        Assert.That(eventsAdded.Last().Contains((eventId.Result).ToString()), Is.EqualTo(true));
    }


    private ICollection<string> AddEventToOutbox()
    {
        ICollection<string> events = new List<string>();
        MongoClient client = new MongoClient(_connectionStringCommandRepoMongo);
        IMongoDatabase database = client.GetDatabase("users"); //cqrs_command
        IMongoCollection<BsonDocument> collection = database.GetCollection<BsonDocument>("events")!;

        for (int i = 0; i < 15; i++)
        {
            events.Add(
               $@"
                    {{
                      ""event_id"": ""{Guid.NewGuid().ToString()}"",
                      ""occured_at"": ""2025-11-29T17:15:00Z"",
                      ""aggregate_name"": ""Product"",
                      ""status"": ""PENDING"",
                      ""event_type"": ""INSERT"",
                      ""payload"": {{
                            ""product_id"": ""{Guid.NewGuid().ToString()}"",
                            ""name"": ""Wireless Mechanical Keyboard"",
                            ""sku"": ""KB-WM-001"",    
                            ""price"": {i},    
                            ""stock_level"": {i},
                            ""is_active"": true
                        }}
                    }}
                ");
        }

        foreach(string eventItem in events)
        {
            collection.InsertOne(BsonDocument.Parse(eventItem));
        }

        return events;
    }
}