using Infrastructure.Persistence.QueryRepository;
using Infrastructure.WebApi;
using Main.Initialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Driver.Core.Configuration;

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
    .AddObserver()
    // TODO MAKE THIS PART OF THE WEBAPP BUILDER
    .AddQueries() // TODO do this serperated
    .AddUseCases(); // TODOD ook apart doen

// TODOD kijken om de webappbuilder te gebruiken een webapp te maken en deze toe tevoegen
// // of te starten in de build/services
WebApplicationBuilder webAppBuilder = WebApplication.CreateBuilder();

foreach (var service in syncBuilder.GetServices()) // TODO look for a better solution 
{
    webAppBuilder.Services.Add(service);
}

webAppBuilder.Services.AddEndpointsApiExplorer();

webAppBuilder.Services
    .AddOpenApi(options =>
    {
        options.AddDocumentTransformer((document, context, _) =>
        {
            document.Info = Routes.OpenApiInfo;
            return Task.CompletedTask;
        });
    })
    .AddProblemDetails()
        .AddCors(options =>
        {
            options.AddDefaultPolicy(builder =>
            {
                builder
                    .WithExposedHeaders("*")
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowAnyOrigin();
            });
        });

webAppBuilder.Services.AddScoped<SyncApplication>();

WebApplication webApp = webAppBuilder.Build();

webApp.UseCors();

webApp.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/openapi/v1.json", "v1");
});

webApp.UseHttpsRedirection();

webApp.MapRoutes();
webApp.MapOpenApi();

await MySqlQueryRepository.CreateBasicStructureQueryDatabase(
    syncBuilder.QueryDatabaseName(),
    syncBuilder.ConnectionString(),
    webApp.Services.GetRequiredService<ILogger<MySqlQueryRepository>>()
);

SyncApplication app = webApp.Services.GetRequiredService<SyncApplication>();

Task runningSyncApp = app.RunAsync(CancellationToken.None);

Task runningWebApp = webApp.RunAsync();

await Task.WhenAll(runningSyncApp, runningWebApp);