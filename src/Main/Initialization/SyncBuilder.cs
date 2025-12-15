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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Main.Initialization;

public class SyncBuilder
{
    private readonly ServiceCollection _services = new();
    private readonly ILogger<SyncBuilder> _logger;

    private readonly string _connectionStringCommandDatabase;
    private readonly string _connectionStringQueryDatabase;

    public SyncBuilder(ILogger<SyncBuilder> logger)
    {
        _logger = logger;
        _logger.LogInformation("Initializing SyncBuilder...");

        ConfigurationBuilder configBuilder = new ConfigurationBuilder();
        configBuilder.SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .AddJsonFile("appsettings.Test.json", optional: true, reloadOnChange: false)
            .AddEnvironmentVariables();

        IConfiguration configuration = configBuilder.Build();
        _services.AddSingleton(configuration);

        _services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        string? connectionStringCommandDatabase = Environment.GetEnvironmentVariable("CONNECTION_STRING_COMMAND_DB");
        string? connectionStringQueryDatabase = Environment.GetEnvironmentVariable("CONNECTION_STRING_QUERY_DB");

        if (!string.IsNullOrEmpty(connectionStringCommandDatabase) && !string.IsNullOrEmpty(connectionStringQueryDatabase))
        {
            _logger.LogInformation("Found connection strings in Environment Variables.");
            _connectionStringCommandDatabase = connectionStringCommandDatabase;
            _connectionStringQueryDatabase = connectionStringQueryDatabase;
        }
        else
        {
            _logger.LogInformation("Environment variables not found. Falling back to appsettings configuration.");

            _connectionStringCommandDatabase = configuration["CommandDatabase:ConnectionString"]
            ?? throw new InvalidOperationException("Connection string 'CommandDatabase' not found in configuration.");
            _connectionStringQueryDatabase = configuration["QueryDatabase:ConnectionString"]
            ?? throw new InvalidOperationException("Connection string 'QueryDatabase' not found in configuration.");
        }
    }

    public SyncBuilder AddRepositories()
    {
        _logger.LogInformation("Adding Repositories...");
        _logger.LogInformation("Command DB Connection: {ConnectionString}", _connectionStringCommandDatabase);
        _logger.LogInformation("Query DB Connection: {ConnectionString}", _connectionStringQueryDatabase);

        _services.AddSingleton<ICommandRepository>(sp => new MongoDbCommandRepository(_connectionStringCommandDatabase, sp.GetRequiredService<ILogger<MongoDbCommandRepository>>()));
        _services.AddSingleton<IQueryRepository>(sp => new MySqlQueryRepository(_connectionStringQueryDatabase, sp.GetRequiredService<ILogger<MySqlQueryRepository>>()));

        return this;
    }

    public SyncBuilder AddEventFactory()
    {
        _logger.LogInformation("Adding Event Factory...");
        _services.AddSingleton<IEventFactory, MySqlEventFactory>();
        return this;
    }

    public SyncBuilder AddProjector()
    {
        _logger.LogInformation("Adding Projector...");
        _services.AddSingleton<Projector>();
        return this;
    }

    public SyncBuilder AddRecovery()
    {
        _logger.LogInformation("Adding Recovery...");
        _services.AddSingleton<Recovery>();
        return this;
    }

    public SyncBuilder AddObserver()
    {
        _logger.LogInformation("Adding Observer...");
        _services.AddSingleton<IObserver>(sp => new MongoDbObserver(_connectionStringCommandDatabase, sp.GetRequiredService<ILogger<MongoDbObserver>>()));
        return this;
    }

    public SyncApplication Build()
    {
        _logger.LogInformation("Building Application...");
        _services.AddSingleton<SyncApplication>();

        ServiceProvider provider = _services.BuildServiceProvider();
        _logger.LogInformation("Application has finished building.");

        return provider.GetRequiredService<SyncApplication>();
    }
}