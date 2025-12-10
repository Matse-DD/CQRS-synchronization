using Application.Contracts.Events.Factory;
using Application.Contracts.Observer;
using Application.Contracts.Persistence;
using Infrastructure.Events.Mappings.MySQL;
using Infrastructure.Observer;
using Infrastructure.Persistence.CommandRepository;
using Infrastructure.Persistence.QueryRepository;
using Infrastructure.Projectors;
using Infrastructure.Recover;

const string connectionStringQueryRepoMySql = "Server=localhost;Port=13306;Database=cqrs_read;User=root;Password=;";
const string connectionStringCommandRepoMongo = "mongodb://localhost:27017/?connect=direct&replicaSet=rs0";

ICommandRepository commandRepository = new MongoDbCommandRepository(connectionStringCommandRepoMongo);
IQueryRepository queryRepository = new MySqlQueryRepository(connectionStringQueryRepoMySql);

IEventFactory eventFactory = new MySqlEventFactory();

Projector projector = new Projector(commandRepository, queryRepository, eventFactory);

Recovery recover = new Recovery(commandRepository, queryRepository, projector);
recover.Recover();

IObserver observer = new MongoDbObserver(connectionStringCommandRepoMongo);

await observer.StartListening(projector.AddEvent, CancellationToken.None);