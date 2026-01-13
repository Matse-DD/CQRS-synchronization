using Infrastructure.Persistence;

namespace Application.Contracts.Persistence;

public interface IQueryRepository
{
    Task<Guid> GetLastSuccessfulEventId();
    Task Execute(PersistenceCommandInfo command, Guid eventId);
    Task Clear();
}