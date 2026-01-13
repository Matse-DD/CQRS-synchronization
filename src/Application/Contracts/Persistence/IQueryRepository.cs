namespace Application.Contracts.Persistence;

public interface IQueryRepository
{
    Task<Guid> GetLastSuccessfulEventId();
    Task Execute(object command, Guid eventId);
    Task Clear();
}