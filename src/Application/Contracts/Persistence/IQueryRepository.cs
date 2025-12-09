namespace Application.Contracts.Persistence;

public interface IQueryRepository
{
    public Task<Guid> GetLastSuccessfulEventId();
    public void Execute(string command, Guid eventId);
}
