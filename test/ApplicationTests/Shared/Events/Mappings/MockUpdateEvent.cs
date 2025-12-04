using Application.Contracts.Events.EventOptions;
using Application.Contracts.Events.Factory;

namespace ApplicationTests.Shared.Events.Mappings;

public class MockUpdateEvent(IntermediateEvent intermediateEvent) : UpdateEvent(intermediateEvent)
{
    public override string GetCommand()
    {
        return $"update {EventId.ToString()}";
    }
}