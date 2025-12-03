using Application.Contracts.Events;
using System.Text.Json;

namespace Infrastructure.Events.Mappings.MySQL;

public class MySqlEventFactory : IEventFactory
{
    public Event DetermineEvent(string incomingEvent)
    {
        IntermediateEvent? intermediateEvent = JsonSerializer.Deserialize<IntermediateEvent>(incomingEvent);

        return intermediateEvent?.EventType switch
        {
            EventType.INSERT => new MySqlInsertEvent(intermediateEvent),
            EventType.DELETE => new MySqlDeleteEvent(intermediateEvent),
            EventType.UPDATE => new MySqlUpdateEvent(intermediateEvent),
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}