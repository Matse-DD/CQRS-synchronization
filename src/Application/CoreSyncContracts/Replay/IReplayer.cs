namespace Application.CoreSyncContracts.Replay;

public interface IReplayer
{
    public Task ReplayTillEvent(string eventId);
}
