using Application.Contracts.Events.Factory;
using System.Text.Json;

namespace Application.Contracts.Events.EventOptions;

public abstract class DeleteEvent : Event
{
    public Dictionary<string, string> Condition { get; init; }

    public DeleteEvent(IntermediateEvent intermediateEvent) : base(intermediateEvent)
    {
        JsonElement conditionElement = intermediateEvent.Payload.GetProperty("condition");
        Condition = conditionElement.Deserialize<Dictionary<string, string>>()!;
    }
}
