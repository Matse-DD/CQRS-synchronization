using Application.Contracts.Events.EventOptions;
using Application.Contracts.Persistence;
using Application.WebApi.Contracts.Filters;
using Application.WebApi.Contracts.Ports;

namespace Application.WebApi.Events;

public sealed record GetEventsByFiltersInput(string? Status, DateTime? BeforeTime);

public sealed class GetEventsByFilters(
    IGetEventsByFiltersQuery getEventsByFiltersQuery
) : IUseCase<GetEventsByFiltersInput, Task<IReadOnlyList<Event>>>
{
    private readonly IGetEventsByFiltersQuery _getEventsByFiltersQuery = getEventsByFiltersQuery;

    public async Task<IReadOnlyList<Event>> Execute(GetEventsByFiltersInput input)
    {
        IReadOnlyList<Event> cqrsEvents = await _getEventsByFiltersQuery.Fetch(CqrsEventExpressions.CqrsEventsBasedOnStatusAndBeforeTime(input.Status, input.BeforeTime));

        return cqrsEvents;
    }
}
