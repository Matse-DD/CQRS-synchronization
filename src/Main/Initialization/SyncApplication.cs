using Application.Contracts.Observer;
using Infrastructure.Projectors;
using Infrastructure.Recover;
using Infrastructure.Replay;

namespace Main.Initialization;

public class SyncApplication(Replayer replay, Recovery recover, IObserver observer, Projector projector)
{
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        //recover.Recover(); // TODO kijken wat we juist gaan doen met recover & replay
        replay.Replay();
        await observer.StartListening(projector.AddEvent, cancellationToken);
    }
}