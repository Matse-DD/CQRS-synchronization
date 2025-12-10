using Application.Contracts.Events.Factory;
using System.Text.Json;

namespace Application.Contracts.Events.EventOptions;

public abstract class UpdateEvent : Event
{
    protected Dictionary<string, string> Condition { get; init; }
    protected Dictionary<string, string> Change { get; init; }

    protected UpdateEvent(IntermediateEvent intermediateEvent) : base(intermediateEvent)
    {
        JsonElement conditionElement = intermediateEvent.Payload.GetProperty("condition");
        JsonElement changeElement = intermediateEvent.Payload.GetProperty("change");
        Condition = conditionElement.Deserialize<Dictionary<string, string>>()!;
        Change = changeElement.Deserialize<Dictionary<string, string>>()!;
    }
}
