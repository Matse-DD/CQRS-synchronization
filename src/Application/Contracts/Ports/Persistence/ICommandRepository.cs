using Application.Contracts.Events;

namespace Application.Contracts.Ports.Persistence;

public interface ICommandRepository
{
    public ICollection<Event> GetAllEvents();
    public void RemoveEvent(Guid eventId);

}
