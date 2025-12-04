namespace Application.Contracts.Persistence;

public interface ICommandRepository
{
    public ICollection<OutboxEvent> GetAllEvents();

    public void RemoveEvent(Guid eventId);

}
