namespace Application.Contracts.Persistence;

public interface ICommandRepository
{
    public Task<ICollection<OutboxEvent>> GetAllEvents();
    public Task<bool> RemoveEvent(Guid eventId);
    public Task<bool> MarkAsDone(Guid eventId);
}
