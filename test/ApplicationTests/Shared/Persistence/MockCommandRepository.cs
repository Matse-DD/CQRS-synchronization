using Application.Contracts.Persistence;

namespace ApplicationTests.Shared.Persistence;

public class MockCommandRepository(ICollection<OutboxEvent> seededEvents) : ICommandRepository
{
    private ICollection<OutboxEvent> _eventOutbox = seededEvents;

    public ICollection<OutboxEvent> GetAllEvents()
    {
        return _eventOutbox;
    }

    public void RemoveEvent(Guid eventId)
    {
        _eventOutbox = _eventOutbox.Where(item => item.eventId != eventId.ToString()).ToList();
    }
}
