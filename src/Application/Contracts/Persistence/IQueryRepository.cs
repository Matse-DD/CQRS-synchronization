namespace Application.Contracts.Persistence;

public interface IQueryRepository
{
    Task<Guid> GetLastSuccessfulEventId();
    Task Execute(string command, Guid eventId);
}