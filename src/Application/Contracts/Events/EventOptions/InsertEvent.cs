using Application.Contracts.Events.Factory;
using System.Text.Json;

namespace Application.Contracts.Events.EventOptions;

public abstract class InsertEvent(IntermediateEvent intermediateEvent) : Event(intermediateEvent)
{
    protected Dictionary<string, object> Properties { get; init; } = intermediateEvent.Payload.Deserialize<Dictionary<string, object>>()!;
}
