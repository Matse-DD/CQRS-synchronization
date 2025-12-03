using System.Text.Json;

namespace Application.Contracts.Events;

public abstract class DeleteEvent : Event
{
    public Dictionary<string, object>? Condition { get; init; }

    public DeleteEvent(IntermediateEvent intermediateEvent) : base(intermediateEvent)
    {
        JsonElement conditionElement = intermediateEvent.Payload.GetProperty("condition");
        Condition = conditionElement.Deserialize<Dictionary<string, object>>();
    }
}
