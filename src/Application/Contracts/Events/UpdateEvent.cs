using System.Text.Json;

namespace Application.Contracts.Events;

public abstract class UpdateEvent : Event
{
    public Dictionary<string, string>? Condition { get; init; }
    public Dictionary<string, string>? Change { get; init; }

    public UpdateEvent(IntermediateEvent intermediateEvent) : base(intermediateEvent)
    {
        JsonElement conditionElement = intermediateEvent.Payload.GetProperty("condition");
        JsonElement changeElement = intermediateEvent.Payload.GetProperty("change");
        Condition = conditionElement.Deserialize<Dictionary<string, string>>();
        Change = changeElement.Deserialize<Dictionary<string, string>>();
    }
}
