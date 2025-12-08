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

    public async void StartRecovering()
    {
        IEnumerable<OutboxEvent> outboxEvents = commandRepository.GetAllEvents();
        Guid lastSuccessfulEventId = queryRepository.GetLastSuccessfulEventId();

        IList<string> pureEvents = [];
        outboxEvents = outboxEvents.ToList().Where(entry => !entry.eventId.Equals(lastSuccessfulEventId.ToString()));
        // mogelijks ook kijken of event done zie mock query repo voor ideen

        outboxEvents.ToList().ForEach(entry => pureEvents.Add(entry.eventItem));

        projector.AddEventsToFront(pureEvents);

        projector.Unlock();
    }
}
