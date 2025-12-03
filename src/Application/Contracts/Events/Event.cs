namespace Application.Contracts.Events;

public abstract class Event(IntermediateEvent intermediateEvent)
{
    private Guid EventId { get; init; }
    public DateTime OccuredAt { get; init; }
    public string AggregateName { get; init; }
    public Status Status { get; init; } 
    public EventType EventType { get; init; }

    public abstract string GetCommand();
}