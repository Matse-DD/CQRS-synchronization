using Application.Ports.Persistence;

namespace ApplicationTests.Shared.Persistence;

public class MockQueryRepository : IQueryRepository
{
    public ICollection<string> History { get; private set; } = [];
    private Guid _lastSuccesfulEventId;

    public void Execute(string command, Guid eventId)
    {
        string lowerCommand = command.ToLower();

        if (lowerCommand.Contains("update") || lowerCommand.Contains("delete") || lowerCommand.Contains("insert"))
        {
            History.Add(command);
            _lastSuccesfulEventId = eventId;
        }
    }

    public Guid GetLastSuccessfulEventId()
    {
        return this._lastSuccesfulEventId;
    }
}