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
        //replay.Replay(); // TODO use this in a seperate branch so we can call upon this with a webapi 
        await observer.StartListening(projector.AddEvent, cancellationToken);
    }
}