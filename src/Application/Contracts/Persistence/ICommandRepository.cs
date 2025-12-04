using Application.Contracts.Events.EventOptions;

namespace Application.Contracts.Persistence;

public interface ICommandRepository
{
    public ICollection<Event> GetAllEvents();
    public void RemoveEvent(Guid eventId);

}
