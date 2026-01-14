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
}