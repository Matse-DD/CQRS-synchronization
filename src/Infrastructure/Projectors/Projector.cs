using Application.Contracts.Events.EventOptions;
using Application.Contracts.Events.Factory;
using Application.Contracts.Persistence;
using System.Collections.Concurrent;
using System.Threading.Channels;

namespace Infrastructure.Projectors;

public class Projector
{
    private readonly ICommandRepository _commandRepository;
    private readonly IQueryRepository _queryRepository;
    private readonly IEventFactory _eventFactory;

    private volatile bool _locked = false;
    private ConcurrentQueue<string> _eventQueue = new ConcurrentQueue<string>();
    private readonly Channel<bool> _signalChannel;

    public Projector(ICommandRepository commandRepository, IQueryRepository queryRepository, IEventFactory eventFactory)
    {
        _commandRepository = commandRepository;
        _queryRepository = queryRepository;
        _eventFactory = eventFactory;
        _signalChannel = Channel.CreateUnbounded<bool>();
        _ = ProcessEvents(); // Normaal moet dit await zijn maar we kunnen de constructor niet async maken, de compiler geeft een waarschuwing als hier geen discard zit
    }

    public void AddEventsToFront(IEnumerable<string> batchOfEvents)
    {
        IList<string> eventList = [.. batchOfEvents, .. _eventQueue];
        _eventQueue = new ConcurrentQueue<string>(eventList.Distinct());
        _signalChannel.Writer.TryWrite(true);
    }

    public void AddEvent(string incomingEvent)
    {
        _eventQueue.Enqueue(incomingEvent);
        _signalChannel.Writer.TryWrite(true);
    }

    private async Task ProjectEvent(string eventToProject)
    {
        try 
        {
            Event convertedEvent = _eventFactory.DetermineEvent(eventToProject);
            
            string commandForEvent = convertedEvent.GetCommand();
            Guid eventId = convertedEvent.EventId;
            
            await _queryRepository.Execute(commandForEvent, eventId);
            await _commandRepository.RemoveEvent(eventId);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"error met de projector: {ex.Message}");
        }
    }

    private async Task ProcessEvents()
    {
        await foreach (bool _ in _signalChannel.Reader.ReadAllAsync())
        {
            if (_locked) continue;

            while (!_locked && _eventQueue.TryDequeue(out string? currentEvent))
            {
                await ProjectEvent(currentEvent);
            }
        }
    }

    public void Lock() => _locked = true;
    public void Unlock()
    {
        _locked = false;
        _signalChannel.Writer.TryWrite(true);
    }
}