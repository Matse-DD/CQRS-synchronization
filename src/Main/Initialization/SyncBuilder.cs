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
    private readonly IConfiguration _configuration;

    public SyncBuilder()
    {
        ConfigurationBuilder configBuilder = new();
        
        configBuilder.SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .AddJsonFile("appsettings.Test.json", optional: true, reloadOnChange: false)
            .AddEnvironmentVariables();

        _configuration = configBuilder.Build();
        _services.AddSingleton(_configuration);

        _services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });
    }

    public SyncBuilder AddRepositories()
    {
        string mongoConn = _configuration["CommandDatabase:ConnectionString"] 
        ?? throw new InvalidOperationException("Missing CommandDatabase ConnectionString");
            
        string mysqlConn = _configuration["QueryDatabase:ConnectionString"] 
        ?? throw new InvalidOperationException("Missing QueryDatabase ConnectionString");
        
        _services.AddSingleton<ICommandRepository>(sp => new MongoDbCommandRepository(mongoConn, sp.GetRequiredService<ILogger<MongoDbCommandRepository>>()));
        _services.AddSingleton<IQueryRepository>(sp => new MySqlQueryRepository(mysqlConn, sp.GetRequiredService<ILogger<MySqlQueryRepository>>()));

        return this;
    }

    public SyncBuilder AddEventFactory()
    {
        _services.AddSingleton<IEventFactory, MySqlEventFactory>(); 
        return this;
    }

    public SyncBuilder AddProjector()
    {
        _services.AddSingleton<Projector>();
        return this;
    }

    public SyncBuilder AddRecovery()
    {
        _services.AddSingleton<Recovery>();
        return this;
    }

    public SyncBuilder AddObserver()
    {
        string mongoConn = _configuration["CommandDatabase:ConnectionString"] 
        ?? throw new InvalidOperationException("Missing CommandDatabase ConnectionString");
        
        _services.AddSingleton<IObserver>(sp => new MongoDbObserver(mongoConn, sp.GetRequiredService<ILogger<MongoDbObserver>>()));
            
        return this;
    }

    public SyncApplication Build()
    {
        _services.AddSingleton<SyncApplication>();
        
        ServiceProvider provider = _services.BuildServiceProvider();
        return provider.GetRequiredService<SyncApplication>();
    }
}