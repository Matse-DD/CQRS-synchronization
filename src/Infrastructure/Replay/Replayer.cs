using Application.Contracts.Persistence;
using Application.CoreSyncContracts.Replay;
using Application.Shared.Exceptions;
using Infrastructure.Projectors;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using System.Text.Json;

namespace Infrastructure.Replay;

public class Replayer(ICommandRepository commandRepository, IQueryRepository queryRepository, Projector projector, ILogger<IReplayer> logger) : IReplayer
{
    public void Replay() // TODO dit zal mogelijks weg mogen
    {
        projector.Lock();
        _ = StartReplaying();
    }

    private async Task StartReplaying() // TODO dit zou ook mogelijks weg mogen
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

    public async Task ReplayTillEvent(string eventId)
    {
        IEnumerable<OutboxEvent> outboxEvents = (await commandRepository.GetAllEvents());
        OutboxEvent? outboxEventToReplayTo = outboxEvents.FirstOrDefault(outboxEvent => outboxEvent.EventId == eventId) ?? throw new NotFoundException($"EventId {eventId} not found in events");

        DateTime timeToReplayTo = JsonDocument.Parse(outboxEventToReplayTo.EventItem).RootElement.GetProperty("occured_at").GetDateTime();

        IEnumerable<OutboxEvent> outboxEventsToReplay = outboxEvents.Where(
            outboxEvent => JsonDocument.Parse(outboxEvent.EventItem).RootElement.GetProperty("occured_at").GetDateTime() <= timeToReplayTo
        );

        await ReplayEvents(outboxEventsToReplay);
    }

    private async Task ReplayEvents(IEnumerable<OutboxEvent> eventsToReplayTo)
    {
        try
        {
            projector.Lock();
            projector.ClearQueue();

            await queryRepository.Clear();

            projector.AddEventsToFront(eventsToReplayTo.Select(e => e.EventItem));
            projector.Unlock();
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error during replay: {Message}", e.Message);
        }
    }
}