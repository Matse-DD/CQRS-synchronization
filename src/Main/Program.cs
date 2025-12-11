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

string connectionStringQueryRepoMySql = config.GetConnectionString("ReadDatabase") ?? "";
string connectionStringCommandRepoMongo = config.GetConnectionString("WriteDatabase") ?? "";

ICommandRepository commandRepository = new MongoDbCommandRepository(connectionStringCommandRepoMongo);
IQueryRepository queryRepository = new MySqlQueryRepository(connectionStringQueryRepoMySql);

IEventFactory eventFactory = new MySqlEventFactory();

Projector projector = new Projector(commandRepository, queryRepository, eventFactory);

Recovery recover = new Recovery(commandRepository, queryRepository, projector);
recover.Recover();

IObserver observer = new MongoDbObserver(connectionStringCommandRepoMongo);

await observer.StartListening(projector.AddEvent, CancellationToken.None);