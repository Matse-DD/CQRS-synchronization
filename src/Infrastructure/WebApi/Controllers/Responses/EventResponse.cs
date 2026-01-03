namespace Infrastructure.WebApi.Controllers.Responses;

public sealed record EventResponse(
    Guid EventId,
    DateTime OccuredAt,
    string AggregateName,
    string Status,
    string EventType,
    string Command
);
