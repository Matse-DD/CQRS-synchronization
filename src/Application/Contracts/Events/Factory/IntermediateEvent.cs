using Application.Contracts.Events.Enums;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Application.Contracts.Events.Factory;

public class IntermediateEvent
{
    [JsonPropertyName("event_id")]
    public Guid EventId { get; init; }

    [JsonPropertyName("occured_at")]
    public DateTime OccuredAt { get; init; }

    [JsonPropertyName("aggregate_name")]
    public required string AggregateName { get; init; }

    [JsonPropertyName("status")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public Status Status { get; init; }

    [JsonPropertyName("event_type")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public EventType EventType { get; init; }

    [JsonPropertyName("payload")]
    public JsonElement Payload { get; init; }
}
