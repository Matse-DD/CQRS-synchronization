using Main.Initialization;

SyncApplication app = await new SyncBuilder()
    .AddRepositories()
    .AddEventFactory()
    .AddProjector()
    .AddRecovery()
    .AddReplay()
    .AddObserver()
    .Build();

await app.RunAsync(CancellationToken.None);