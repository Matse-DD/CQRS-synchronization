using Application.Contracts.Events;
using Infrastructure.Events.Mappings.MySQL;
using System.Text.Json;

namespace Infrastructure.Events;

public class MySqlEventFactory : IEventFactory
{

    public Event DetermineEvent(string incomingEvent)
    {
        Dictionary<string, object>? propertiesEvent = JsonSerializer.Deserialize<Dictionary<string, object>>(incomingEvent);
        EventType eventType = JsonSerializer.Deserialize<EventType>(propertiesEvent?["event_type"].ToString() ?? string.Empty);

        return eventType switch
        {
            EventType.INSERT => new MySqlInsertEvent(incomingEvent),
            // EventType.DELETE => new MySqlDeleteEvent(incomingEvent);
            // EventType.UPDATE => new MySqlUpdateEvent(incomingEvent);
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}

