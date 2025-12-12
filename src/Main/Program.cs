using Main.Initialization;
using Microsoft.Extensions.Logging;

using ILoggerFactory loggerFactory = LoggerFactory.Create(
    builder =>
    {
        builder.AddConsole(); builder.SetMinimumLevel(LogLevel.Information);
    });

var logger = loggerFactory.CreateLogger<SyncBuilder>();

SyncBuilder syncBuilder = new(logger);

SyncApplication app = syncBuilder
    .AddRepositories()
    .AddEventFactory()
    .AddProjector()
    .AddRecovery()
    .AddObserver()
    .Build();

await app.RunAsync(CancellationToken.None);