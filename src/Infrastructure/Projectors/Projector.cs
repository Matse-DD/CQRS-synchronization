using Application.Contracts.Events;
using Application.Contracts.Events.Enums;
using Application.Contracts.Events.EventOptions;
using Application.Contracts.Events.Factory;
using Application.Contracts.Persistence;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Threading.Channels;

namespace Infrastructure.Projectors;

public class Projector
{
    private readonly ICommandRepository _commandRepository;
    private readonly IQueryRepository _queryRepository;
    private readonly IEventFactory _eventFactory;
    private readonly ILogger<Projector> _logger;
    private readonly ISchemaBuilder _schemaBuilder;

    private volatile bool _locked = false;
    private ConcurrentQueue<string> _eventQueue = new ConcurrentQueue<string>();
    private readonly Channel<bool> _signalChannel;

    public Projector(
        ICommandRepository commandRepository,
        IQueryRepository queryRepository,
        IEventFactory eventFactory,
        ILogger<Projector> logger,
        ISchemaBuilder schemaBuilder)
    {
        _commandRepository = commandRepository;
        _queryRepository = queryRepository;
        _eventFactory = eventFactory;
        _logger = logger;
        _schemaBuilder = schemaBuilder;
        _signalChannel = Channel.CreateUnbounded<bool>();

        _logger.LogInformation("Projector started. Waiting for events...");
        _ = ProcessEvents();
    }

    public void AddEventsToFront(IEnumerable<string> newEvents)
    {
        IList<string> eventList = [.. newEvents, .. _eventQueue];

        _eventQueue = new ConcurrentQueue<string>(eventList.Distinct());

        _logger.LogInformation("Added {Count} events to the front of the queue.", newEvents.Count());
        _signalChannel.Writer.TryWrite(true);
    }

    public void AddEvent(string incomingEvent)
    {
        _eventQueue.Enqueue(incomingEvent);
        _logger.LogDebug("Enqueued event: {EventSnippet}...", incomingEvent);
        _signalChannel.Writer.TryWrite(true);
    }

    public void Lock()
    {
        _locked = true;
        _logger.LogWarning("Projector LOCKED.");
    }

    public void Unlock()
    {
        _locked = false;
        _logger.LogInformation("Projector UNLOCKED.");
        _signalChannel.Writer.TryWrite(true);
    }

    public void ClearQueue()
    {
        _eventQueue.Clear();
    }

    private async Task ProcessEvents()
    {
        await foreach (bool _ in _signalChannel.Reader.ReadAllAsync())
        {
            if (CanProcess())
            {
                await ProjectQueuedEvents();
            }
        }
    }

    private bool CanProcess()
    {
        if (_locked)
        {
            _logger.LogDebug("Projector is locked. Skipping processing cycle.");
            return false;
        }

        return true;
    }

    private async Task ProjectQueuedEvents()
    {
        while (!_locked && _eventQueue.TryDequeue(out string? currentEvent))
        {
            await ProjectEvent(currentEvent);
        }
    }

    private async Task ProjectEvent(string eventToProject)
    {
        try
        {
            Event convertedEvent = _eventFactory.DetermineEvent(eventToProject);
            await HandleSchema(convertedEvent);
            await Project(convertedEvent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error projecting event: {Message}", ex.Message);
        }
    }
    private async Task HandleSchema(Event convertedEvent)
    {
        if (convertedEvent.EventType == EventType.INSERT)
        {
            await _schemaBuilder.Create(_queryRepository, (InsertEvent)convertedEvent);
        }
    }

    private async Task Project(Event convertedEvent)
    {
        string commandForEvent = convertedEvent.GetCommand();
        Guid eventId = convertedEvent.EventId;

        _logger.LogDebug("Projecting Event {EventId}", eventId);

        await _queryRepository.Execute(commandForEvent, eventId);
        await _commandRepository.MarkAsDone(eventId);

        _logger.LogInformation("Successfully projected Event {EventId}", eventId);
    }
}