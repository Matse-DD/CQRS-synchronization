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
    private ICommandRepository? _commandRepository;
    private IQueryRepository? _queryRepository;
    private IEventFactory? _eventFactory;
    private Projector? _projector;
    private Recovery? _recovery;
    private IObserver? _observer;

    private readonly string _connectionStringCommandDatabase;
    private readonly string _connectionStringQueryDatabase;

    public SyncBuilder()
    {
        string? connectionStringCommandDatabase = Environment.GetEnvironmentVariable("CONNECTION_STRING_COMMAND_DB");
        string? connectionStringQueryDatabase = Environment.GetEnvironmentVariable("CONNECTION_STRING_QUERY_DB");

        if (connectionStringCommandDatabase == null || connectionStringQueryDatabase == null)
        {
            Console.WriteLine("connectionStringCommandDatabase or connectionStringQueryDatabase not found falling back to appsettings.Test.json");
            
            IConfiguration config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
                .AddJsonFile($"appsettings.Test.json", optional: false, reloadOnChange: false)
                .Build();

            _connectionStringCommandDatabase = config["CommandDatabase:ConnectionString"] 
                ?? throw new InvalidOperationException("Connection string 'CommandDatabase' not found.");

            _connectionStringQueryDatabase = config["QueryDatabase:ConnectionString"]
                ?? throw new InvalidOperationException("Connection string 'QueryDatabase' not found.");
        }
        else
        {
            _connectionStringCommandDatabase = connectionStringCommandDatabase;
            _connectionStringQueryDatabase = connectionStringQueryDatabase;
        }
    }

    public SyncBuilder AddRepositories()
    {
        _commandRepository = new MongoDbCommandRepository(_connectionStringCommandDatabase);
        _queryRepository = new MySqlQueryRepository(_connectionStringQueryDatabase);

        Console.WriteLine(_connectionStringCommandDatabase);
        Console.WriteLine(_connectionStringQueryDatabase);

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
        string connectionStringCommandRepoMongo = _connectionStringCommandDatabase;

        _observer = new MongoDbObserver(connectionStringCommandRepoMongo);
        return this;
    }

    public SyncApplication Build()
    {
        return new SyncApplication(_recovery!, _observer!, _projector!);
    }
}
