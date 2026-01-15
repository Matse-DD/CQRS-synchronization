using Application.Contracts.Observer;
using Infrastructure.Projectors;
using Infrastructure.Recover;
using Infrastructure.Replay;

namespace Main.Initialization;

public class SyncApplication(Replayer replayer, Recovery recovery, IObserver observer, Projector projector)
{
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        _ = recovery.Recover();
        //_ = replayer.Replay(); // This can be used instead of recover it replays all events instead of just the pending events.
        await observer.StartListening(projector.AddEvent, cancellationToken);
    }
}