using Application.Contracts.Events.EventOptions;
using Application.Contracts.Events.Factory;
using Application.Contracts.Persistence;

namespace ApplicationTests.Shared.Events.Mappings;

public class MockInsertEvent(IntermediateEvent intermediateEvent) : InsertEvent(intermediateEvent)
{
    public override CommandInfo GetCommandInfo()
    {
        return new($"insert {EventId.ToString()}");
    }
}
