using Main.Initialization;

SyncBuilder syncBuilder = new();

SyncApplication app = syncBuilder
    .AddRepositories()
    .AddEventFactory()
    .AddProjector()
    .AddRecovery()
    .AddObserver()
    .Build();

await app.RunAsync(CancellationToken.None);