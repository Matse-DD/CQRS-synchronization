using Application.Contracts.Persistence;
using Infrastructure.Projectors;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Replay;

public class Replayer(ICommandRepository commandRepository, IQueryRepository queryRepository, Projector projector, ILogger<Replayer> logger)
{
    public void Replay()
    {
        projector.Lock();
        StartReplaying();
    }

    private async void StartReplaying()
    {
        try
        {
            IEnumerable<OutboxEvent> outboxEvents = (await commandRepository.GetAllEvents()).Reverse();
            await queryRepository.Clear();

            projector.AddEventsToFront(outboxEvents.Select(e => e.eventItem));
            projector.Unlock();
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error during replay: {Message}", e.Message);
        }
    }
}