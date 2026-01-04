using Application.Contracts.Persistence;
using Application.CoreSyncContracts.Replay;
using Application.Shared.Exceptions;
namespace Application.WebApi.Replay;

public sealed record ReplayTillEventInput(string? EventId);

public sealed class ReplayTillEvent(
    IReplay replayer
) : IUseCase<ReplayTillEventInput, Task>
{
    public Task Execute(ReplayTillEventInput input)
    {
        if (input.EventId == null)
        {
            throw new NotFoundException($"No event id found to replay from {input.EventId}");
        }

        replayer.ReplayTillEvent(input.EventId);

        return Task.CompletedTask;
    }
}