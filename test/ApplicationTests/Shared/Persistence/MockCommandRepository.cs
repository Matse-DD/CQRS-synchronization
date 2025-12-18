using Application.Contracts.Persistence;

namespace ApplicationTests.Shared.Persistence;

public class MockCommandRepository(ICollection<OutboxEvent> seededEvents) : ICommandRepository
{
    private ICollection<OutboxEvent> _eventOutbox = seededEvents;

    public Task<ICollection<OutboxEvent>> GetAllEvents()
    {
        return Task.FromResult(_eventOutbox);
    }

    public void MarkEventAsInProgress(Guid eventId)
    {
        for (int i = 0; i < _eventOutbox.Count; i++)
        {
            OutboxEvent eventEntry = _eventOutbox.ElementAt(i);
            if (eventEntry.EventId == eventId.ToString())
            {
                string newEventItem = eventEntry.EventItem.Replace("\"status\":\"PENDING\"", "\"status\":\"INPROGRESS\"");
                eventEntry = eventEntry with { EventItem = newEventItem };
            }
        }
    }

    public Task<bool> RemoveEvent(Guid eventId)
    {
        _eventOutbox = _eventOutbox.Where(item => item.EventId != eventId.ToString()).ToList();
        return Task.FromResult(true);
    }

    public Task<bool> MarkAsDone(Guid eventId)
    {
        OutboxEvent? currentEvent = _eventOutbox.FirstOrDefault(e => e.EventId == eventId.ToString());
        if (currentEvent == null) return Task.FromResult(false);

        string newEvent = currentEvent.EventItem.Replace("\"status\":\"PENDING\"", "\"status\":\"DONE\"");
        currentEvent = currentEvent with { EventItem = newEvent };
        return Task.FromResult(true);
    }
}
