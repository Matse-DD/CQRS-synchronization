using Application.Contracts.Persistence;
using Infrastructure.Persistence;

namespace ApplicationTests.Shared.Persistence;

public class MockQueryRepository : IQueryRepository
{
    public ICollection<string> History { get; private set; } = [];
    public Guid LastSuccessfulEventId { get; set; }

    public Task Execute(PersistenceCommandInfo commandinfo, Guid eventId)
    {
        string stringCommand = commandinfo.PureCommand;
        string lowerCommand = stringCommand.ToLower();

        if (lowerCommand.Contains("update") || lowerCommand.Contains("delete") || lowerCommand.Contains("insert"))
        {
            History.Add(stringCommand);
            LastSuccessfulEventId = eventId;
        }
        return Task.CompletedTask;

        throw new ArgumentException("Command type not supported");
    }

    public Task Clear()
    {
        History.Clear();
        return Task.CompletedTask;
    }

    public Task<Guid> GetLastSuccessfulEventId()
    {
        return Task.FromResult(LastSuccessfulEventId);
    }
}