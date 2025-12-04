using Application.Contracts.Persistence;

namespace ApplicationTests.Shared.Persistence;

public class MockQueryRepository : IQueryRepository
{
    public ICollection<string> History { get; private set; } = [];
    public Guid LastSuccesfulEventId { get; set; }

    public void Execute(string command, Guid eventId)
    {
        string lowerCommand = command.ToLower();

        if (lowerCommand.Contains("update") || lowerCommand.Contains("delete") || lowerCommand.Contains("insert"))
        {
            History.Add(command);
            LastSuccesfulEventId = eventId;
        }
    }

    public Guid GetLastSuccessfulEventId()
    {
        return this.LastSuccesfulEventId;
    }
}
