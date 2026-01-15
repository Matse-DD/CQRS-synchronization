using Application.Contracts.Persistence;
using Infrastructure.Projectors;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Replay;

public class Replayer(ICommandRepository commandRepository, IQueryRepository queryRepository, Projector projector, ILogger<Replayer> logger)
{
    public Task Replay()
    {
        projector.Lock();
        return StartReplaying();
    }

    private async Task StartReplaying()
    {
        try
        {
            IEnumerable<OutboxEvent> outboxEvents = (await commandRepository.GetAllEvents());
            await queryRepository.Clear();

            projector.AddEventsToFront(outboxEvents.Select(e => e.EventItem));
            projector.Unlock();
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error during replay: {Message}", e.Message);
        }
    }
}