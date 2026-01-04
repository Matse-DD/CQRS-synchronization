using Infrastructure.Persistence.QueryRepository;
using Main.Initialization;
using Main.WebApi;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddConsole(); builder.SetMinimumLevel(LogLevel.Information);
});

SyncBuilder syncBuilder = new SyncBuilder()
    .AddRepositories()
    .AddEventFactory()
    .AddProjector()
    .AddRecovery()
    .AddReplay()
    .AddObserver(); 

WebApplicationBuilder webAppBuilder = WebApplication.CreateBuilder(args);

webAppBuilder.Services.AddSyncInfrastructure(syncBuilder);
webAppBuilder.Services.AddQueries();
webAppBuilder.Services.AddUseCases();
webAppBuilder.Services.AddWebApiModules();

WebApplication webApp = webAppBuilder.Build();

webApp.UseWebApiModules();

await AdditionalServices.SetBasicDatabaseStructureUp(
    syncBuilder, 
    webApp.Services.GetRequiredService<ILogger<MySqlQueryRepository>>()
);

SyncApplication app = webApp.Services.GetRequiredService<SyncApplication>();

Task runningSyncApp = app.RunAsync(CancellationToken.None);
Task runningWebApp = webApp.RunAsync();

await Task.WhenAll(runningSyncApp, runningWebApp);