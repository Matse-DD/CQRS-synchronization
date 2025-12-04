using Application.Contracts.Persistence;

namespace ApplicationTests.Shared.Persistence;

public class MockQueryRepository : IQueryRepository
{
    public ICollection<string> History { get; private set; } = [];
    private Guid lastSuccesfulEventId;

    public void Execute(string command, Guid eventId)
    {
        string lowerCommand = command.ToLower();

        if (lowerCommand.Contains("update") || lowerCommand.Contains("delete") || lowerCommand.Contains("insert"))
        {
            Console.WriteLine(command);
            History.Add(command);
            lastSuccesfulEventId = eventId;
        }
    }

    public Guid GetLastSuccessfulEventId()
    {
        return this.lastSuccesfulEventId;
    }
}
