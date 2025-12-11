using Application.Contracts.Events.Factory;
using Application.Contracts.Observer;
using Application.Contracts.Persistence;
using Infrastructure.Events.Mappings.MySQL;
using Infrastructure.Observer;
using Infrastructure.Persistence.CommandRepository;
using Infrastructure.Persistence.QueryRepository;
using Infrastructure.Projectors;
using Infrastructure.Recover;
using Microsoft.Extensions.Configuration;

var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

IConfiguration config = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", true, false)
    .AddJsonFile($"appsettings.{env}.json", true, false)
    .Build();

//string connectionStringQueryRepoMySql = config.GetConnectionString("ReadDatabase") ?? "";
//string connectionStringCommandRepoMongo = config.GetConnectionString("WriteDatabase") ?? "";

//const string connectionStringQueryRepoMySql = "Server=localhost;Port=13306;Database=cqrs_read;User=root;Password=;";
const string connectionStringQueryRepoMySql = "Server=localhost;Port=40132;Database=users;User=user;Password=userpassword;";
//const string connectionStringCommandRepoMongo = "mongodb://localhost:27017/?connect=direct&replicaSet=rs0";
const string connectionStringCommandRepoMongo = "mongodb://localhost:40131/?directConnection=true&replicaSet=rs0";

ICommandRepository commandRepository = new MongoDbCommandRepository(connectionStringCommandRepoMongo);
IQueryRepository queryRepository = new MySqlQueryRepository(connectionStringQueryRepoMySql);

IEventFactory eventFactory = new MySqlEventFactory();

Projector projector = new Projector(commandRepository, queryRepository, eventFactory);

Recovery recover = new Recovery(commandRepository, queryRepository, projector);
recover.Recover();

IObserver observer = new MongoDbObserver(connectionStringCommandRepoMongo);

await observer.StartListening(projector.AddEvent, CancellationToken.None);