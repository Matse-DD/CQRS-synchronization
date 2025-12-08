using Application.Contracts.Persistence;

namespace ApplicationTests.Shared.Persistence;

public class MockCommandRepository(ICollection<OutboxEvent> seededEvents) : ICommandRepository
{
    private ICollection<OutboxEvent> _eventOutbox = seededEvents;

    public ICollection<OutboxEvent> GetAllEvents()
    {
        return _eventOutbox;
    }

    public void MarkEventAsInProgress(Guid eventId)
    {
        for(int i = 0; i < _eventOutbox.Count; i++) {
            OutboxEvent eventEntry = _eventOutbox.ElementAt(i);
            if (eventEntry.eventId == eventId.ToString())
            {
                string newEventItem = eventEntry.eventItem.Replace("\"status\":\"PENDING\"", "\"status\":\"INPROGRESS\"");
                eventEntry = eventEntry with { eventItem = newEventItem };
            }
        }
    }

    public void RemoveEvent(Guid eventId)
    {
        _eventOutbox = _eventOutbox.Where(item => item.eventId != eventId.ToString()).ToList();
    }
}
