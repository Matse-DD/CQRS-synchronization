namespace Application.Contracts.Persistence;

public record OutboxEvent(string EventId, string EventItem);
