using Application.Contracts.Events.EventOptions;
using Application.Contracts.Events.Factory;
using Infrastructure.Persistence;

namespace ApplicationTests.Shared.Events.Mappings;

public class MockUpdateEvent(IntermediateEvent intermediateEvent) : UpdateEvent(intermediateEvent)
{
    public override PersistenceCommandInfo GetCommandInfo()
    {
        return new($"update {EventId.ToString()}");
    }
}