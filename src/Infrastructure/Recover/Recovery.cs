using Application.Contracts.Persistence;
using Infrastructure.Projectors;

namespace Infrastructure.Recover;

public class Recovery(ICommandRepository commandRepository, IQueryRepository queryRepository, Projector projector)
{
    public void Recover()
    {
        projector.Lock();

        StartRecovering();
    }

    private async void StartRecovering()
    {
        try
        {
            IEnumerable<OutboxEvent> outboxEvents = await commandRepository.GetAllEvents();
            Guid lastSuccessfulEventId = await queryRepository.GetLastSuccessfulEventId();

            if (lastSuccessfulEventId != Guid.Empty)
            {
                outboxEvents = outboxEvents.ToList().Where(entry => !entry.eventId.Equals(lastSuccessfulEventId.ToString()));
            }

            IList<string> pureEvents = [];
            // mogelijks ook kijken of event done zie mock query repo voor ideen

            outboxEvents.ToList().ForEach(entry => pureEvents.Add(entry.eventItem));
            projector.AddEventsToFront(pureEvents);
            projector.Unlock();
        }
        catch (Exception e)
        {
            Console.WriteLine($"error in recovery: {e.Message}");
        }
    }
}
