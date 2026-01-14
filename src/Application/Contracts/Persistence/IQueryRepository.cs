using Infrastructure.Persistence;

namespace Application.Contracts.Persistence;

public interface IQueryRepository
{
    Task<Guid> GetLastSuccessfulEventId();
    Task Execute(CommandInfo command, Guid eventId);
    Task Clear();
    Task ExecuteSchemaCommand(string command);
    Task ExecuteString(string command, Guid eventId);
}