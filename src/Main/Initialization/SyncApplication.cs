using Application.Contracts.Observer;
using Infrastructure.Projectors;
using Infrastructure.Recover;

namespace Main.Initialization;

public class SyncApplication(Recovery recovery, IObserver observer, Projector projector)
{
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        recovery.Recover();
        await observer.StartListening(projector.AddEvent, cancellationToken);
    }
}