using Application.Contracts.Events.EventOptions;
using Application.Contracts.Persistence;

namespace ApplicationTests.Shared.Persistence;

public class MockCommandRepository : ICommandRepository
{
    private Dictionary<string, string> _eventOutbox = new();

    public ICollection<string> GetAllEvents()
    {
        return _eventOutbox.Values;
    }

    public void RemoveEvent(Guid eventId)
    {
        _eventOutbox.Remove(eventId.ToString());
    }
}
