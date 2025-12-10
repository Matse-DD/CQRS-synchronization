
using Application.Contracts.Events.Enums;
using Application.Contracts.Events.EventOptions;
using Application.Contracts.Events.Factory;
using System.Text.Json;

namespace ApplicationTests.Shared.Events.Mappings;

public class MockEventFactory() : IEventFactory
{
    public Event DetermineEvent(string incomingEvent)
    {
        IntermediateEvent? intermediateEvent = JsonSerializer.Deserialize<IntermediateEvent>(incomingEvent);

        return intermediateEvent?.EventType switch
        {
            EventType.Insert => new MockInsertEvent(intermediateEvent),
            EventType.Delete => new MockDeleteEvent(intermediateEvent),
            EventType.Update => new MockUpdateEvent(intermediateEvent),
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}
