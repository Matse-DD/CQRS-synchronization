using Application.Contracts.Events.Enums;
using Application.Contracts.Events.Factory;

namespace Application.Contracts.Events.EventOptions;

public abstract class Event(IntermediateEvent intermediateEvent)
{
    public Guid EventId { get; init; } = intermediateEvent.EventId;
    public DateTime OccuredAt { get; init; } = intermediateEvent.OccuredAt;
    public string AggregateName { get; init; } = intermediateEvent.AggregateName;
    public Status Status { get; init; } = intermediateEvent.Status;
    public EventType EventType { get; init; } = intermediateEvent.EventType;

    public abstract string GetCommand();
}