using Main.Initialization;
using Microsoft.Extensions.Logging;

using ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddConsole(); builder.SetMinimumLevel(LogLevel.Information);
});

ILogger<SyncBuilder> logger = loggerFactory.AddSeq(Environment.GetEnvironmentVariable("SEQ_SERVER_URL") ?? "http://localhost:5341", Environment.GetEnvironmentVariable("SEQ_API_KEY")).CreateLogger<SyncBuilder>();

SyncBuilder syncBuilder = new(logger);

SyncApplication app = await syncBuilder
    .AddRepositories()
    .AddEventFactory()
    .AddProjector()
    .AddRecovery()
    .AddReplay()
    .AddObserver()
    .Build();

await app.RunAsync(CancellationToken.None);