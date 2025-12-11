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

namespace Main.Initialization;

public class SyncBuilder
{
    private readonly IConfiguration _configuration;
    private ICommandRepository? _commandRepository;
    private IQueryRepository? _queryRepository;
    private IEventFactory? _eventFactory;
    private Projector? _projector;
    private Recovery? _recovery;
    private IObserver? _observer;

    public SyncBuilder()
    {
        string? env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

        _configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .AddJsonFile($"appsettings.{env}.json", optional: true, reloadOnChange: false)
            .Build();
    }

    public SyncBuilder AddRepositories()
    {
        string connectionStringQueryRepoMySql = _configuration.GetConnectionString("ReadDatabase")
        ?? throw new InvalidOperationException("Connection string 'ReadDatabase' not found.");

        string connectionStringCommandRepoMongo = _configuration.GetConnectionString("WriteDatabase")
        ?? throw new InvalidOperationException("Connection string 'WriteDatabase' not found.");

        _commandRepository = new MongoDbCommandRepository(connectionStringCommandRepoMongo);
        _queryRepository = new MySqlQueryRepository(connectionStringQueryRepoMySql);
        return this;
    }

    public SyncBuilder AddEventFactory()
    {
        _eventFactory = new MySqlEventFactory();
        return this;
    }

    public SyncBuilder AddProjector()
    {
        _projector = new Projector(_commandRepository!, _queryRepository!, _eventFactory!);
        return this;
    }

    public SyncBuilder AddRecovery()
    {
        _recovery = new Recovery(_commandRepository!, _queryRepository!, _projector!);
        return this;
    }

    public SyncBuilder AddObserver()
    {
        string connectionStringCommandRepoMongo = _configuration.GetConnectionString("WriteDatabase")
        ?? throw new InvalidOperationException("Connection string 'WriteDatabase' not found.");

        _observer = new MongoDbObserver(connectionStringCommandRepoMongo);
        return this;
    }

    public SyncApplication Build()
    {
        return new SyncApplication(_recovery!, _observer!, _projector!);
    }
}
