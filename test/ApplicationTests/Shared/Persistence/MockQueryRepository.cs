using Application.Contracts.Persistence;

namespace ApplicationTests.Shared.Persistence;

public class MockQueryRepository : IQueryRepository
{
    public ICollection<string> History { get; private set; } = [];
    public Guid LastSuccessfulEventId { get; set; }

    public Task Execute(string command, Guid eventId)
    {
        string lowerCommand = command.ToLower();

        if (lowerCommand.Contains("update") || lowerCommand.Contains("delete") || lowerCommand.Contains("insert"))
        {
            History.Add(command);
            LastSuccessfulEventId = eventId;
        }
        return Task.CompletedTask;
    }

    public Task Clear()
    {
        throw new NotImplementedException();
    }

    public Task<Guid> GetLastSuccessfulEventId()
    {
        return Task.FromResult(LastSuccessfulEventId);
    }
}