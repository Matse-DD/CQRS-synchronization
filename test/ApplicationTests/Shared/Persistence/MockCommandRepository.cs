using Application.Contracts.Events;
using Application.Contracts.Ports.Persistence;

namespace ApplicationTests.Shared.Persistence;

public class MockCommandRepository : ICommandRepository
{
    private Dictionary<string, Event> _eventOutbox = new();

    public ICollection<Event> GetAllEvents()
    {
        return _eventOutbox.Values;
    }

    public void RemoveEvent(Guid eventId)
    {
        _eventOutbox.Remove(eventId.ToString());
    }
}
