using Application.Contracts.Events.EventOptions;

namespace Infrastructure.WebApi.Controllers.Responses;

public sealed record GetEventsByFiltersResponse(
    IEnumerable<Event> Data
);
