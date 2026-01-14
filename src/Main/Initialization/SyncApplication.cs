using Application.Contracts.Observer;
using Infrastructure.Projectors;
using Infrastructure.Recover;
using Infrastructure.Replay;

namespace Main.Initialization;

public class SyncApplication(Replayer replay, Recovery recover, IObserver observer, Projector projector)
{
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        recover.Recover();
        //replay.Replay(); // This can be used instead of recover it replays all events instead of just the pending events.
        await observer.StartListening(projector.AddEvent, cancellationToken);
    }
}