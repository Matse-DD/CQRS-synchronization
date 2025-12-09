
using Application.Contracts.Events.Factory;
using Application.Contracts.Persistence;
using DotNet.Testcontainers.Builders;
using Infrastructure.Events.Mappings.MySQL;
using Infrastructure.Observer;
using Infrastructure.Persistence.CommandRepository;
using Infrastructure.Persistence.QueryRepository;
using Infrastructure.Projectors;
using Infrastructure.Recover;

namespace IntegrationTests;

public class ProjectorIntegration
{
    private readonly string _connectionStringQueryRepoMySql = "Server=mysql;Port=13306;Database=cqrs_read;User=root;Password=;";
    private readonly string _connectionStringCommandRepoMongo = "mongodb://mongo:27017/?replicaSet=rs0";
    
    [OneTimeSetUp]
    public void Set_DatabasesUp()
    {
        // Create a new instance of a container.
        var container = new ContainerBuilder()
            .WithCommand()
          // Wait until the HTTP endpoint of the container is available.
          .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(r => r.ForPort(8080)))
          // Build the container configuration.
          .Build();

    }

    [Test]
    public void Projector_Should_Project()
    {
        // Set infrastructure up
        ICommandRepository commandRepo = new MongoDbCommandRepository(_connectionStringCommandRepoMongo);
        IQueryRepository queryRepo = new MySqlQueryRepository(_connectionStringQueryRepoMySql);

        IEventFactory eventFactory = new MySqlEventFactory();

        Projector projector = new Projector(commandRepo, queryRepo, eventFactory);


        // start listing for changes
        MongoDbObserver observer = new MongoDbObserver(_connectionStringCommandRepoMongo);
        observer.StartListening(projector.ProjectEvent);
        Recovery recover = new Recovery(commandRepo, queryRepo, projector);




    }
}
