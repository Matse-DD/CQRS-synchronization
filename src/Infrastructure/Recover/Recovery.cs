using Application.Contracts.Persistence;
using Infrastructure.Projectors;
using Microsoft.Extensions.Logging;
using SharpCompress.Common;

namespace Infrastructure.Recover;

public class Recovery(ICommandRepository commandRepository, IQueryRepository queryRepository, Projector projector, ILogger<Recovery> logger)
{

    public void Recover()
    {
        logger.LogInformation("Initiating Recovery Process...");
        projector.Lock();

        _ = StartRecovering();
    }

    private async Task StartRecovering()
    {
        try
        {
            IList<string> eventsToRecover = (await GetEventsToRecover()).ToList();

            logger.LogInformation("Replaying {Count} events to Projector...", eventsToRecover.Count);

            projector.AddEventsToFront(eventsToRecover);
            logger.LogInformation("Events successfully added to Projector for recovery. Recovery process done.");
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error during recovery: {Message}", e.Message);
        }
        finally
        {
            projector.Unlock();
        }
    }

    private async Task<IEnumerable<string>> GetEventsToRecover()
    {
        IEnumerable<OutboxEvent> outboxEvents = await GetPendingEventsFromOutbox();
        Guid lastSuccessfulEventId = await GetLastSuccessfulEventId();

        if (IsLastEventIdSet(lastSuccessfulEventId))
        {
            logger.LogInformation("Found last checkpoint: {EventId}. Filtering outbox...", lastSuccessfulEventId);
            outboxEvents = DetermineEventsToRecover(outboxEvents, lastSuccessfulEventId);
        }
        else
        {
            logger.LogInformation("No checkpoint found. Replaying all events.");
        }

        return ExtractEvents(outboxEvents);
    }

    private async Task<IEnumerable<OutboxEvent>> GetPendingEventsFromOutbox()
    {
        logger.LogInformation("Fetching all events from Command Repository...");
        return await commandRepository.GetAllEvents();
    }

    private async Task<Guid> GetLastSuccessfulEventId()
    {
        logger.LogInformation("Fetching last successful event ID from Query Repository...");
        return await queryRepository.GetLastSuccessfulEventId();
    }

    private IEnumerable<OutboxEvent> DetermineEventsToRecover(IEnumerable<OutboxEvent> outboxEvents, Guid lastSuccessfulEventId)
    {
        return outboxEvents.Where(outboxEvent => !IsAlreadyProcessed(lastSuccessfulEventId, outboxEvent));
    }

    private static bool IsAlreadyProcessed(Guid lastSuccessfulEventId, OutboxEvent outboxEvent)
    {
        return outboxEvent.EventId.Equals(lastSuccessfulEventId.ToString()) || outboxEvent.EventItem.Contains("\"status\" : \"DONE\"");
    }

    private static bool IsLastEventIdSet(Guid lastSuccessfulEventId)
    {
        return lastSuccessfulEventId != Guid.Empty;
    }

    private static IEnumerable<string> ExtractEvents(IEnumerable<OutboxEvent> outboxEvents)
    {
        return outboxEvents.Select(outboxEvent => outboxEvent.EventItem);
    }
}