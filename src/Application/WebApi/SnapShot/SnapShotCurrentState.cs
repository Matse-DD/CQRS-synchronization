using Application.Contracts.Persistence;
using Application.CoreSyncContracts.SnapShot;

namespace Application.WebApi.SnapShot;

public sealed record SnaShotCurrentStateInput();

public sealed class SnapShotCurrentState(
    ISnapshoter snapshoter
)
{
}
