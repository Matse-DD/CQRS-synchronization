using Application.Contracts.Events;
using Infrastructure.Events.Mappings.MySQL;
using System.Text.Json;

namespace Infrastructure.Events;

public class MySqlEventFactory : IEventFactory
{

    public Event DetermineEvent(string incomingEvent)
    {
        Dictionary<string, object> propertiesEvent = JsonSerializer.Deserialize<Dictionary<string, object>>(incomingEvent);
        EventType eventType = JsonSerializer.Deserialize<EventType>(propertiesEvent["event_type"].ToString());

        switch (eventType)
        {
            case EventType.INSERT:
                return new MySqlInsertEvent(incomingEvent);

            //case EventType.UPDATE:
            //    return new MySqlUpdateEvent(incomingEvent);

            //case EventType.DELETE:
            //    return new MySqlDeleteEvent(incomingEvent);
            
        }

        return null;
    }
}

