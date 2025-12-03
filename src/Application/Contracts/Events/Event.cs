using Application.Contracts.Events.Payloads;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Application.Contracts.Events;

public abstract class Event(string incomingEvent)
{
    [JsonPropertyName("event_id")]
    private Guid EventId { get; init; }
  
    [JsonPropertyName("occured_at")]
    public DateTime OccuredAt { get; init; }

    [JsonPropertyName("aggregate_name")]
    public string AggregateName { get; init; }

    [JsonPropertyName("status")]
    public Status Status { get; init; } 

    [JsonPropertyName("event_type")]
    public EventType EventType { get; init; }

    public IPayload PayLoad { get; set; }

    public abstract string GetCommand();
}