using Application.Contracts.Events.Enums;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Application.Contracts.Events.Factory;

public class IntermediateEvent
{
    [JsonPropertyName("id")]
    public Guid EventId { get; init; }

    [JsonPropertyName("occurredAt")]
    public DateTime OccurredAt { get; init; }

    [JsonPropertyName("aggregateName")]
    public required string AggregateName { get; init; }

    [JsonPropertyName("status")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public Status Status { get; init; }

    [JsonPropertyName("eventType")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public EventType EventType { get; init; }

    [JsonPropertyName("payload")]
    public JsonElement Payload { get; init; }
}
