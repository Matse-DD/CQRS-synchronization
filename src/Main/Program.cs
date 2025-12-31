using Infrastructure.WebApi;
using Main.Initialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddConsole(); builder.SetMinimumLevel(LogLevel.Information);
});

SyncApplication app = await new SyncBuilder()
    .AddRepositories()
    .AddEventFactory()
    .AddProjector()
    .AddRecovery()
    .AddReplay()
    .AddObserver()
    .Build();

// TODOD kijken om de webappbuilder te gebruiken een webapp te maken en deze toe tevoegen
// // of te starten in de build/services
WebApplicationBuilder webAppBuilder = WebApplication.CreateBuilder();

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


Task runningSyncApp = app.RunAsync(CancellationToken.None);

Task runningWebApp = webApp.RunAsync();

await Task.WhenAll(runningSyncApp, runningWebApp);