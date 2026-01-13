using Application.Contracts.Persistence;
using Infrastructure.Persistence.CommandRepository;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;
using MongoDB.Driver;

namespace IntegrationTests.Persistence;

public class TestMongoDbEventOperations
{
    private const string ConnectionStringCommandRepoMongo = "mongodb://localhost:27017/users?connect=direct&replicaSet=rs0";
    private MongoDbCommandRepository _repository;
    private IMongoCollection<BsonDocument> _collection;

    [SetUp]
    public async Task SetUp()
    {}}