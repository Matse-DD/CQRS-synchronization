using Application.Contracts.Events.EventOptions;
using Application.Contracts.Events.Factory;
using Application.Contracts.Persistence;
using System.Collections.Concurrent;

namespace Infrastructure.Projectors;

public class Projector
{
    private readonly ICommandRepository _commandRepository;
    private readonly IQueryRepository _queryRepository;
    private readonly IEventFactory _eventFactory;

    public bool locked = false;
    private ConcurrentQueue<string> _eventQueue = new ConcurrentQueue<string>();

    public Projector(ICommandRepository commandRepository, IQueryRepository queryRepository, IEventFactory eventFactory)
    {
        _commandRepository = commandRepository;
        _queryRepository = queryRepository;
        _eventFactory = eventFactory;

        ProcessEvents();
    }

    public void AddEventsToFront(IEnumerable<string> batchOfEvents)
    {
        IList<string> eventList = [.. batchOfEvents, .. _eventQueue];
        _eventQueue = new ConcurrentQueue<string>(eventList.Distinct());
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
            if (locked || _eventQueue.Count == 0) await Task.Delay(250);

            else
            {
                if (_eventQueue.TryDequeue(out string? currentEvent))
                {
                    ProjectEvent(currentEvent);
                }
            }
        }
    }

    public void Lock()
    {
        locked = true;
    }

    public void Unlock()
    {
        locked = false;
    }
}
