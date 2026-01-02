using Application.Contracts.Events.Factory;
using System.Text.Json;

namespace Application.Contracts.Events.EventOptions;

public abstract class DeleteEvent : Event
{
    protected Dictionary<string, string> Condition { get; init; }

    protected DeleteEvent(IntermediateEvent intermediateEvent) : base(intermediateEvent)
    {
        JsonElement conditionElement = intermediateEvent.Payload.GetProperty("condition");
        Condition = conditionElement.Deserialize<Dictionary<string, string>>()!;
    }
}
