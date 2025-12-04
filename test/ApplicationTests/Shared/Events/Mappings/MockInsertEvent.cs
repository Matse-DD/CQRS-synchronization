using Application.Contracts.Events.EventOptions;
using Application.Contracts.Events.Factory;

namespace ApplicationTests.Shared.Events.Mappings;

public class MockInsertEvent(IntermediateEvent intermediateEvent) : InsertEvent(intermediateEvent)
{
    public override string GetCommand()
    {
        return $"insert {EventId.ToString()}";
    }
}
