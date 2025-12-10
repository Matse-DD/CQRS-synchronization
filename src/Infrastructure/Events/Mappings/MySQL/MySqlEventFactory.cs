using Application.Contracts.Events.Enums;
using Application.Contracts.Events.EventOptions;
using Application.Contracts.Events.Factory;
using System.Text.Json;

namespace Infrastructure.Events.Mappings.MySQL;

public class MySqlEventFactory : IEventFactory
{
    public Event DetermineEvent(string incomingEvent)
    {
        IntermediateEvent? intermediateEvent = JsonSerializer.Deserialize<IntermediateEvent>(incomingEvent);

        return intermediateEvent?.EventType switch
        {
            EventType.Insert => new MySqlInsertEvent(intermediateEvent),
            EventType.Delete => new MySqlDeleteEvent(intermediateEvent),
            EventType.Update => new MySqlUpdateEvent(intermediateEvent),
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}