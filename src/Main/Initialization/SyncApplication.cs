using Application.Contracts.Observer;
using Application.CoreSyncContracts.Replay;
using Infrastructure.Projectors;
using Infrastructure.Recover;

namespace Main.Initialization;

public class SyncApplication(IReplay replay, Recovery recover, IObserver observer, Projector projector)
{
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        recover.Recover();
        //replay.Replay(); // TODO use this in a seperate branch so we can call upon this with a webapi 
        await observer.StartListening(projector.AddEvent, cancellationToken);
    }
}