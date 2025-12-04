using Application.Contracts.Persistence;
using Infrastructure.Projectors;

namespace Infrastructure.Recover;

public class Recovery(ICommandRepository commandRepository, IQueryRepository queryRepository, Projector projector)
{
    public void Recover()
    {
        projector.Lock();

        IEnumerable<OutboxEvent> outboxEvents = commandRepository.GetAllEvents();
        Guid lastSuccessfulEventId = queryRepository.GetLastSuccessfulEventId();

        IList<string> pureEvents = [];
        outboxEvents = outboxEvents.ToList().Where(entry => !entry.eventId.Equals(lastSuccessfulEventId.ToString()));
        outboxEvents.ToList().ForEach(entry => pureEvents.Add(entry.eventItem));

        projector.AddEventsToFront(pureEvents);

        projector.Unlock();
    }
}
