namespace Application.CoreSyncContracts.Replay;

public interface IReplay
{
    public Task ReplayTillEvent(string eventId);
}
