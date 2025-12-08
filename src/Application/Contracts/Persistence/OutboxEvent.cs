namespace Application.Contracts.Persistence;

public record OutboxEvent(string eventId, string eventItem);
