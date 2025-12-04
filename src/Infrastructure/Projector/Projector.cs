using Application.Contracts.Events.EventOptions;
using Application.Contracts.Events.Factory;
using Application.Contracts.Persistence;

namespace Infrastructure.Projector;

public class Projector(ICommandRepository commandRepository, IQueryRepository queryRepository, IEventFactory eventFactory)
{
    private readonly ICommandRepository _commandRepository = commandRepository;
    private readonly IQueryRepository _queryRepository = queryRepository;
    private readonly IEventFactory _eventFactory = eventFactory;

    public bool Locked { private get; set; }

    private ICollection<string> eventQueue = new List<string>();


    public void AddEventsToFront(IEnumerable<string> batchOfEvents)
    {
        eventQueue = [.. batchOfEvents, ..eventQueue];

        eventQueue = eventQueue.Distinct().ToList();
    }

    public void AddEvent(string incomingEvent)
    {
        eventQueue.Add(incomingEvent);
    }

    public void ProjectEvent(string eventToProject)
    {
        Event convertedEvent = _eventFactory.DetermineEvent(eventToProject);

        string commandForEvent = convertedEvent.GetCommand();
        Guid eventId = convertedEvent.EventId;

        _queryRepository.Execute(commandForEvent, eventId);
        _commandRepository.RemoveEvent(eventId);
    }

    public async void ProcessEvents()
    {
        if (Locked || ) return;


    }




}
