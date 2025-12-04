using Application.Contracts.Events.EventOptions;
using Application.Contracts.Events.Factory;
using Application.Contracts.Persistence;

namespace Infrastructure.Projectors;

public class Projector
{
    private readonly ICommandRepository _commandRepository;
    private readonly IQueryRepository _queryRepository;
    private readonly IEventFactory _eventFactory;

    public bool Locked { private get; set; } = false;
    private Queue<string> _eventQueue = new Queue<string>();

    public Projector(ICommandRepository commandRepository, IQueryRepository queryRepository, IEventFactory eventFactory)
    {
        _commandRepository = commandRepository;
        _queryRepository = queryRepository;
        _eventFactory = eventFactory;

        ProcessEvents();
    }

    public void AddEventsToFront(IEnumerable<string> batchOfEvents)
    {
        IList<string> eventList = new List<string>();

        eventList = [.. batchOfEvents, .. _eventQueue];
        _eventQueue = new Queue<string>(eventList.Distinct());
    }

    public void AddEvent(string incomingEvent)
    {
        _eventQueue.Enqueue(incomingEvent);
    }

    public void ProjectEvent(string eventToProject)
    {
        Event convertedEvent = _eventFactory.DetermineEvent(eventToProject);

        string commandForEvent = convertedEvent.GetCommand();
        Guid eventId = convertedEvent.EventId;

        _queryRepository.Execute(commandForEvent, eventId);
        _commandRepository.RemoveEvent(eventId);
    }

    private async void ProcessEvents()
    {
        while (true)
        {
            if (Locked || _eventQueue.Count == 0) await Task.Delay(250);

            else
            {
                string currentEvent = _eventQueue.Dequeue();
                ProjectEvent(currentEvent);
            }
        }
    }
}
