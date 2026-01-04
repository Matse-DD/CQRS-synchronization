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
    .AddObserver(); //TODO check if there is noway to just add the syncapplication

// TODOD kijken om de webappbuilder te gebruiken een webapp te maken en deze toe tevoegen
// // of te starten in de build/services
WebApplicationBuilder webAppBuilder = WebApplication.CreateBuilder(args);

foreach (var service in syncBuilder.GetServices()) // TODO look for a better solution 
{
    webAppBuilder.Services.Add(service);
}

webAppBuilder.Services.AddQueries();
webAppBuilder.Services.AddUseCases();

webAppBuilder.Services.AddWebApiModules();

webAppBuilder.Services.AddScoped<SyncApplication>();

WebApplication webApp = webAppBuilder.Build();
webApp.UseWebApiModules();

await MySqlQueryRepository.CreateBasicStructureQueryDatabase(
    syncBuilder.QueryDatabaseName(),
    syncBuilder.ConnectionString(),
    webApp.Services.GetRequiredService<ILogger<MySqlQueryRepository>>()
);

SyncApplication app = webApp.Services.GetRequiredService<SyncApplication>();

Task runningSyncApp = app.RunAsync(CancellationToken.None);

Task runningWebApp = webApp.RunAsync();

await Task.WhenAll(runningSyncApp, runningWebApp);