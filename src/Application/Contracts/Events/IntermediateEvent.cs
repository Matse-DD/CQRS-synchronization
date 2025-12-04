using System.Text.Json;
using System.Text.Json.Serialization;

namespace Application.Contracts.Events;

public class IntermediateEvent
{
    [JsonPropertyName("event_id")]
    public Guid EventId { get; init; }

    [JsonPropertyName("occured_at")]
    public DateTime OccuredAt { get; init; }

    [JsonPropertyName("aggregate_name")]
    public string AggregateName { get; init; }

    [JsonPropertyName("status")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public Status Status { get; init; }

    [JsonPropertyName("event_type")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public EventType EventType { get; init; }

    [JsonPropertyName("payload")]
    public JsonElement Payload { get; init; }
}
