using Application.Contracts.Persistence;
using Infrastructure.Projectors;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Recover;

public class Recovery(ICommandRepository commandRepository, IQueryRepository queryRepository, Projector projector, ILogger<Recovery> logger)
{

    public void Recover()
    {
        logger.LogInformation("Initiating Recovery Process...");
        projector.Lock();

        StartRecovering();
    }

    private async void StartRecovering()
    {
        try
        {
            logger.LogInformation("Fetching all events from Command Repository...");
            IEnumerable<OutboxEvent> outboxEvents = await commandRepository.GetAllEvents();

            logger.LogInformation("Fetching last successful event ID from Query Repository...");
            Guid lastSuccessfulEventId = await queryRepository.GetLastSuccessfulEventId();

            if (lastSuccessfulEventId != Guid.Empty)
            {
                logger.LogInformation("Found last checkpoint: {EventId}. Filtering outbox...", lastSuccessfulEventId);
                outboxEvents = outboxEvents.ToList().Where(entry => !entry.eventId.Equals(lastSuccessfulEventId.ToString()));
            }
            else
            {
                logger.LogInformation("No checkpoint found. Replaying all events.");
            }

            IList<string> pureEvents = [];

            List<OutboxEvent> eventList = outboxEvents.ToList();
            eventList.ForEach(entry => pureEvents.Add(entry.eventItem));

            logger.LogInformation("Replaying {Count} events to Projector...", pureEvents.Count);

            projector.AddEventsToFront(pureEvents);
            projector.Unlock();

            logger.LogInformation("Events successfully added to Projector for recovery. Recovery process done.");
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error during recovery: {Message}", e.Message);
        }
    }
}