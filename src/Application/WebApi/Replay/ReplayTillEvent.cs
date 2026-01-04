using Application.Contracts.Persistence;
using Application.CoreSyncContracts.Replay;
using Application.Shared.Exceptions;
using Microsoft.Extensions.Logging;

namespace Application.WebApi.Replay;
public sealed record ReplayTillEventInput(string? EventId);

public sealed class ReplayTillEvent(
    IReplay replayer,
    ILogger<ReplayTillEvent> logger
) : IUseCase<ReplayTillEventInput, Task>
{
    private ILogger<ReplayTillEvent> _logger = logger;

    public Task Execute(ReplayTillEventInput input)
    {
        _logger.LogInformation("Start replaying");

        if (input.EventId == null)
        {
            throw new NotFoundException($"No event id found to replay from {input.EventId}");
        }

        replayer.ReplayTillEvent(input.EventId);

        _logger.LogInformation("Replayed till {EventId} ", input.EventId);

        return Task.CompletedTask;
    }
}