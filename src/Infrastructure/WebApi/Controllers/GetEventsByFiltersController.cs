using Application.Contracts.Events.EventOptions;
using Application.WebApi;
using Application.WebApi.Contracts.Ports;
using Application.WebApi.Events;
using Infrastructure.WebApi.Controllers.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Infrastructure.WebApi.Controllers;

public sealed class GetEventsByFiltersController : IController
{
    public static async Task<Results<Ok<GetEventsByFiltersResponse>, UnauthorizedHttpResult>> Invoke(
        [AsParameters] GetEventsByFiltersParameters parameters,
        [FromServices] IUseCase<GetEventsByFiltersInput, Task<IReadOnlyList<Event>>> getEventsByFilters
    )
    {
        GetEventsByFiltersInput input = new(
                parameters.Status,
                parameters.BeforeTime
            );

        IReadOnlyList<Event> cqrsEvents = await getEventsByFilters.Execute(input);

        return TypedResults.Ok(BuildResponse(cqrsEvents));
    }

    private static GetEventsByFiltersResponse BuildResponse(IReadOnlyList<Event> cqrsEvents)
    {
        return new GetEventsByFiltersResponse(
            cqrsEvents.Select(cqrsEvent =>
                new EventResponse(
                    cqrsEvent.EventId,
                    cqrsEvent.OccuredAt,
                    cqrsEvent.AggregateName,
                    cqrsEvent.Status.ToString(),
                    cqrsEvent.EventType.ToString(),
                    cqrsEvent.GetCommand()
                )
            )
        );
    }
}

public sealed record GetEventsByFiltersParameters
{
    public required string? Status { get; init; }
    public required DateTime? BeforeTime { get; init; }
}

