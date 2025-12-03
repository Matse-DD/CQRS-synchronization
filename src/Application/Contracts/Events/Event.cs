namespace Application.Contracts.Events;

public abstract class Event(Guid eventId, DateTime occuredAt, string aggregateName, Status status, EventType eventType)
{
    private Guid EventId { get; init; } = eventId;
    public DateTime OccuredAt { get; init; } = occuredAt;
    public string AggregateName { get; init; } = aggregateName;
    public Status Status { get; init; } = status;
    public EventType EventType { get; init; } = eventType;

    public abstract string GetCommand();
    //Payload will be implemented later
}