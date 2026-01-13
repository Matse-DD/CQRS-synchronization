using Application.Contracts.Events.EventOptions;
using Application.Contracts.Events.Factory;
using Infrastructure.Persistence;

namespace ApplicationTests.Shared.Events.Mappings;

public class MockDeleteEvent(IntermediateEvent intermediateEvent) : DeleteEvent(intermediateEvent)
{
    public override PersistenceCommandInfo GetCommandInfo()
    {
        return new($"delete {EventId.ToString()}");
    }
}
