namespace Application.CoreSyncContracts.SnapShot;

public interface ISnapshoter
{
    public Task TakeSnapShotOfCurrentState();
}
