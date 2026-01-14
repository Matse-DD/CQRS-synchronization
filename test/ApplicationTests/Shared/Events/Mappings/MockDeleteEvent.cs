using Application.Contracts.Events.EventOptions;
using Application.Contracts.Events.Factory;
using Application.Contracts.Persistence;

namespace ApplicationTests.Shared.Events.Mappings;

public class MockDeleteEvent(IntermediateEvent intermediateEvent) : DeleteEvent(intermediateEvent)
{
    public override CommandInfo GetCommandInfo()
    {
        return new($"delete {EventId.ToString()}");
    }
}
