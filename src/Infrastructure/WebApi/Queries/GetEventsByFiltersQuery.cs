using Application.Contracts.Events.EventOptions;
using Application.Contracts.Events.Factory;
using Application.Contracts.Persistence;
using Application.WebApi.Contracts.Ports;
using System.Linq.Expressions;

namespace Infrastructure.WebApi.Queries;

public sealed class GetEventsByFiltersQuery(
    ICommandRepository commandRepository,
    IEventFactory eventFactory
) : IGetEventsByFiltersQuery
{
    private readonly ICommandRepository _commandRepository = commandRepository;
    private readonly IEventFactory _eventFactory = eventFactory;

    public async Task<IReadOnlyList<Event>> Fetch(Expression<Func<Event, bool>> filter)
    {
        ICollection<OutboxEvent> outboxEvents = await _commandRepository.GetAllEvents();

        ICollection<Event> events = new List<Event>();

        foreach (OutboxEvent outboxEvent in outboxEvents)
        {
            Event determinedEvent = _eventFactory.DetermineEvent(outboxEvent.EventItem);
            events.Add(determinedEvent);
        }

        Func<Event, bool> usableFilter = filter.Compile();
        return events.Where(usableFilter).ToList();
    }
}
