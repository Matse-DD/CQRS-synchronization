using Application.Contracts.Events.EventOptions;
using Application.Contracts.Events.Factory;
using Application.Contracts.Persistence;

namespace ApplicationTests.Shared.Events.Mappings;

public class MockUpdateEvent(IntermediateEvent intermediateEvent) : UpdateEvent(intermediateEvent)
{
    public override CommandInfo GetCommandInfo()
    {
        return new($"update {EventId.ToString()}");
    }
}