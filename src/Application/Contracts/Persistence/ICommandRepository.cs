namespace Application.Contracts.Persistence;

public interface ICommandRepository
{
    public Task<ICollection<OutboxEvent>> GetAllEvents();
    public Task<bool> RemoveEvent(Guid eventId);
    public Task MarkAsDone(Guid eventId);
}
