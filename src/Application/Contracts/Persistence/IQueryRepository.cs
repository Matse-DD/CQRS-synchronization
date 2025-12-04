namespace Application.Contracts.Persistence;

public interface IQueryRepository
{
    public Guid GetLastSuccessfulEventId();
    public void Execute(string command, Guid eventId);
}
