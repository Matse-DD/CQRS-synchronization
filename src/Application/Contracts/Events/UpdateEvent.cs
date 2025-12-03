using System.Text.Json;

namespace Application.Contracts.Events;

public abstract class UpdateEvent : Event
{
    public Dictionary<string, object> Condition { get; init; }
    public Dictionary<string, object> Change { get; init; }

    public UpdateEvent(IntermediateEvent intermediateEvent) : base(intermediateEvent)
    {
        JsonElement conditionElement = intermediateEvent.Payload.GetProperty("condition");
        JsonElement changeElement = intermediateEvent.Payload.GetProperty("change");
        Condition = conditionElement.Deserialize<Dictionary<string, object>>();
        Change = changeElement.Deserialize<Dictionary<string, object>>();
    }
}
