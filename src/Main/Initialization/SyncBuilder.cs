using Application.Contracts.Events;
using Application.Contracts.Events.Factory;
using Application.Contracts.Observer;
using Application.Contracts.Persistence;
using Infrastructure.Events.Mappings.MySQL;
using Infrastructure.Observer;
using Infrastructure.Persistence.CommandRepository;
using Infrastructure.Persistence.QueryRepository;
using Infrastructure.Projectors;
using Infrastructure.Recover;
using Infrastructure.Replay;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Configuration;

namespace Main.Initialization;

public class SyncBuilder
{
    private readonly ServiceCollection _services = new();
    private readonly ILogger<SyncBuilder> _logger;

    private readonly string _connectionStringCommandDatabase;
    private readonly string _connectionStringQueryDatabase;

    public SyncBuilder()
    {
        IConfiguration configuration = CreateConfiguration();

        _logger = CreateLogger(configuration) ?? throw new InvalidProgramException("No appsettings configuration found.");
        _logger.LogInformation("Initializing SyncBuilder...");

        _services.AddSingleton(configuration);

        _services.AddLogging(builder =>
        {
            ConfigureLogger(builder, configuration);
        });

        (_connectionStringCommandDatabase, _connectionStringQueryDatabase) = DetermineConnectionStrings(configuration);
    }

    private static IConfiguration CreateConfiguration()
    {
        ConfigurationBuilder configBuilder = new ConfigurationBuilder();

        configBuilder.SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .AddJsonFile("appsettings.Test.json", optional: true, reloadOnChange: false)
            .AddEnvironmentVariables();

        return configBuilder.Build();
    }

    private ILogger<SyncBuilder>? CreateLogger(IConfiguration configuration)
    {
        using ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
        {
            ConfigureLogger(builder, configuration);
        });


        return loggerFactory.CreateLogger<SyncBuilder>();
    }

    private ILoggingBuilder ConfigureLogger(ILoggingBuilder builder, IConfiguration configuration)
    {
        builder.AddConsole();
        builder.SetMinimumLevel(LogLevel.Information);

        string url = GetSetting("SEQ_SERVER_URL", "Logger:DefaultUrl", configuration);
        string apiKey = GetSetting("SEQ_API_KEY", "Logger:ApiKey", configuration);

        builder.AddSeq(url, apiKey);

        return builder;
    }

    private string GetSetting(string envVar, string configPath, IConfiguration configuration)
    {
        string? value = Environment.GetEnvironmentVariable(envVar) ?? configuration[configPath];

        if (string.IsNullOrEmpty(value))
        {
            throw new InvalidOperationException($"Configuration '{envVar}' and '{configPath}' is missing expected atleast one.");
        }

        return value;
    }

    private string GetSettingWithLogging(string envVar, string configPath, IConfiguration configuration)
    {
        string? value = GetSetting(envVar, configPath, configuration);

        _logger.LogInformation("Loaded configuration for {SettingName}", envVar);
        return value;
    }

    private (string connectionStringCommandDatabase, string connectionStringQueryDatabase) DetermineConnectionStrings(IConfiguration configuration)
    {
        string connectionStringCommandDatabase = GetSettingWithLogging("CONNECTION_STRING_COMMAND_DB", "CommandDatabase:ConnectionString", configuration);
        string connectionStringQueryDatabase = GetSettingWithLogging("CONNECTION_STRING_QUERY_DB", "QueryDatabase:ConnectionString", configuration);

        return (connectionStringCommandDatabase, connectionStringQueryDatabase);
    }

    public SyncBuilder AddRepositories()
    {
        _logger.LogInformation("Adding Repositories...");
        _logger.LogInformation("Command DB Connection: {ConnectionString}", _connectionStringCommandDatabase);
        _logger.LogInformation("Query DB Connection: {ConnectionString}", _connectionStringQueryDatabase);

        _services.AddSingleton<ICommandRepository>(sp => new MongoDbCommandRepository(_connectionStringCommandDatabase, sp.GetRequiredService<ILogger<MongoDbCommandRepository>>()));
        _services.AddSingleton<IQueryRepository>(sp => new MySqlQueryRepository(_connectionStringQueryDatabase, sp.GetRequiredService<ILogger<MySqlQueryRepository>>()));
        _services.AddSingleton<ISchemaBuilder>(sp => new MySqlSchemaBuilder());
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

    public SyncBuilder AddReplay()
    {
        _logger.LogInformation("Adding Replay...");
        _services.AddSingleton<Replayer>();
        return this;
    }

    public SyncBuilder AddObserver()
    {
        _logger.LogInformation("Adding Observer...");
        _services.AddSingleton<IObserver>(sp => new MongoDbObserver(_connectionStringCommandDatabase, sp.GetRequiredService<ILogger<MongoDbObserver>>()));
        return this;
    }

    public async Task<SyncApplication> Build()
    {
        _logger.LogInformation("Building Application...");
        _services.AddSingleton<SyncApplication>();

        ServiceProvider provider = _services.BuildServiceProvider();
        _logger.LogInformation("Application has finished building.");

        await MySqlQueryRepository.CreateBasicStructureQueryDatabase(_connectionStringQueryDatabase, provider.GetRequiredService<ILogger<MySqlQueryRepository>>());

        return provider.GetRequiredService<SyncApplication>();
    }
}