using Application.Contracts.Events.Enums;
using Application.Contracts.Events.Factory;

namespace Application.Contracts.Events.EventOptions;

public abstract class Event(IntermediateEvent intermediateEvent)
{
    public Guid EventId { get; init; } = intermediateEvent.EventId;
    public DateTime OccuredAt { get; init; } = intermediateEvent.OccurredAt;
    public string AggregateName { get; init; } = intermediateEvent.AggregateName;
    public Status Status { get; init; } = intermediateEvent.Status;
    public EventType EventType { get; init; } = intermediateEvent.EventType;

    public abstract object GetCommand(); //TODO fix object this should be something better

    public override bool Equals(object? obj)
    {
        return obj is Event @event && EventId.Equals(@event.EventId);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(EventId);
    }
}