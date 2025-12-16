using Main.Initialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddConsole(); builder.SetMinimumLevel(LogLevel.Information);
});

ILogger<SyncBuilder> logger = loggerFactory.AddSeq(Environment.GetEnvironmentVariable("SEQ_SERVER_URL") ?? "", Environment.GetEnvironmentVariable("SEQ_API_KEY")).CreateLogger<SyncBuilder>();

SyncBuilder syncBuilder = new(logger);

SyncApplication app = syncBuilder
    .AddRepositories()
    .AddEventFactory()
    .AddProjector()
    .AddRecovery()
    .AddReplay()
    .AddObserver()
    .Build();

await app.RunAsync(CancellationToken.None);