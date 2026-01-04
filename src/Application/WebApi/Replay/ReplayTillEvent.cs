using Application.Contracts.Persistence;
using Application.CoreSyncContracts.Replay;
namespace Application.WebApi.Replay;

public sealed record ReplayTillEventInput(string? EventId);

public sealed class ReplayTillEvent(
    IReplay replayer
) : IUseCase<ReplayTillEventInput, Task>
{
    public Task Execute(ReplayTillEventInput input)
    {
        replayer.ReplayTillEvent(input.EventId);
    }
}