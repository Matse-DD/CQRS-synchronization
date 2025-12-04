using Application.Contracts.Events.EventOptions;
using Application.Contracts.Events.Factory;
using Application.Contracts.Persistence;

namespace Infrastructure.Projector;

public class Projector(ICommandRepository commandRepository, IQueryRepository queryRepository, IEventFactory eventFactory)
{
    public bool Locked { private get; set; }
    private ICollection<string> _eventQueue = new List<string>();


    public void AddEventsToFront(IEnumerable<string> batchOfEvents)
    {
        _eventQueue = [.. batchOfEvents, .. _eventQueue];
        _eventQueue = _eventQueue.Distinct().ToList();
    }

    public void AddEvent(string incomingEvent)
    {
        _eventQueue.Add(incomingEvent);
    }

    public void ProjectEvent(string eventToProject)
    {
        Event convertedEvent = eventFactory.DetermineEvent(eventToProject);

        string commandForEvent = convertedEvent.GetCommand();
        Guid eventId = convertedEvent.EventId;

        queryRepository.Execute(commandForEvent, eventId);
        commandRepository.RemoveEvent(eventId);
    }

    public async void ProcessEvents()
    {
        if (Locked || ) return;


    }
}
