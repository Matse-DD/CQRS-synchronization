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
            if (eventEntry.eventId == eventId.ToString())
            {
                string newEventItem = eventEntry.eventItem.Replace("\"status\":\"PENDING\"", "\"status\":\"INPROGRESS\"");
                eventEntry = eventEntry with { eventItem = newEventItem };
            }
        }
    }

    public Task<bool> RemoveEvent(Guid eventId)
    {
        _eventOutbox = _eventOutbox.Where(item => item.eventId != eventId.ToString()).ToList();
        return Task.FromResult(true);
    }

    public Task MarkAsDone(Guid eventId)
    {
        OutboxEvent currentEvent = _eventOutbox.FirstOrDefault(e => e.eventId == eventId.ToString()) ?? throw new InvalidOperationException("Event not found");
        
        string newEvent = currentEvent.eventItem.Replace("\"status\":\"PENDING\"", "\"status\":\"DONE\"");
        currentEvent = currentEvent with { eventItem = newEvent };
        return Task.CompletedTask;
    }
}
