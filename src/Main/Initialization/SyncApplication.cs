using Application.Contracts.Observer;
using Infrastructure.Projectors;
using Infrastructure.Recover;
using Infrastructure.Replay;

namespace Main.Initialization;

public class SyncApplication(Replayer replay, IObserver observer, Projector projector)
{
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        replay.Replay();
        await observer.StartListening(projector.AddEvent, cancellationToken);
    }
}