using System.Text.Json;

namespace Application.Contracts.Events;

public abstract class InsertEvent : Event
{
    public Dictionary<string, object> Properties { get; init; }
    public InsertEvent(IntermediateEvent intermediateEvent) : base(intermediateEvent)
    {
        Properties = intermediateEvent.Payload.Deserialize<Dictionary<string, object>>();
    }
}
