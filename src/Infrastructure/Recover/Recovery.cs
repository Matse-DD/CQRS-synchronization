using Application.Contracts.Events.Enums;
using Application.Contracts.Persistence;
using Infrastructure.Projectors;
using Microsoft.Extensions.Logging;
using SharpCompress.Common;
using System.Text.Json;

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
        IEnumerable<OutboxEvent> outboxEvents = await GetAllEventsFromOutbox();
        Guid lastSuccessfulEventId = await GetLastSuccessfulEventId();

        logger.LogInformation("Found last checkpoint: {EventId}. Filtering outbox to get PENDING events...", lastSuccessfulEventId);
        outboxEvents = DetermineEventsToRecover(outboxEvents, lastSuccessfulEventId, commandRepository);

        return ExtractEvents(outboxEvents);
    }

    private async Task<IEnumerable<OutboxEvent>> GetAllEventsFromOutbox()
    {
        logger.LogInformation("Fetching all events from Command Repository...");
        return await commandRepository.GetAllEvents();
    }

    private async Task<Guid> GetLastSuccessfulEventId()
    {
        logger.LogInformation("Fetching last successful event ID from Query Repository...");
        return await queryRepository.GetLastSuccessfulEventId();
    }

    private static IEnumerable<OutboxEvent> DetermineEventsToRecover(IEnumerable<OutboxEvent> outboxEvents, Guid lastSuccessfulEventId, ICommandRepository commandRepository)
    {
        return outboxEvents.Where(outboxEvent => HasToBeProcessed(lastSuccessfulEventId, outboxEvent, commandRepository));
    }

    private static bool HasToBeProcessed(Guid lastSuccessfulEventId, OutboxEvent outboxEvent, ICommandRepository commandRepository)
    {
        if (!IsLastEventIdSet(lastSuccessfulEventId)) return IsEventPending(outboxEvent);


        if(IsLastEventId(outboxEvent, lastSuccessfulEventId) && IsEventPending(outboxEvent)){
            commandRepository.MarkAsDone(lastSuccessfulEventId);
            return false;
        }

        return IsEventPending(outboxEvent);
    }

    private static bool IsEventPending(OutboxEvent outboxEvent)
    {
        using JsonDocument eventDoc = JsonDocument.Parse(outboxEvent.EventItem);
        JsonElement eventAsJson = eventDoc.RootElement;

        if (eventAsJson.TryGetProperty("status", out JsonElement statusElement))
        {
            return statusElement.GetString()?.Equals(Status.PENDING.ToString()) ?? false;
        }

        return false;
    }

    private static bool IsLastEventId(OutboxEvent outboxEvent, Guid lastSuccessfulEventId)
    {
        return outboxEvent.EventId.Equals(lastSuccessfulEventId.ToString());
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