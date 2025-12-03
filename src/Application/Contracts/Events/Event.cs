using Application.Contracts.Events.Payloads;
using System.Text.Json.Serialization;

namespace Application.Contracts.Events;

public abstract class Event(Guid eventId, DateTime occuredAt, string aggregateName, Status status, EventType eventType)
{
    [JsonPropertyName("event_id")]
    private Guid EventId { get; init; } = eventId;
    
    [JsonPropertyName("occured_at")]
    public DateTime OccuredAt { get; init; } = occuredAt; 

    [JsonPropertyName("aggregate_name")]
    public string AggregateName { get; init; } = aggregateName;

    [JsonPropertyName("status")]
    public Status Status { get; init; } = status;

    [JsonPropertyName("event_type")]
    public EventType EventType { get; init; } = eventType;

    public IDynamicPayload PayLoad { get; private set; }

    public abstract string GetCommand();
    //Payload will be implemented later
}