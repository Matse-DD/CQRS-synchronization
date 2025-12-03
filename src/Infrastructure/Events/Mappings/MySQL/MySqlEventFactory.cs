using Application.Contracts.Events;
using System.Text.Json;

namespace Infrastructure.Events.Mappings.MySQL;

public class MySqlEventFactory : IEventFactory
{

    public Event DetermineEvent(string incomingEvent)
    {

        IntermediateEvent intermediateEvent = JsonSerializer.Deserialize<IntermediateEvent>(incomingEvent);


        switch (intermediateEvent.EventType)
        {
            case EventType.INSERT:
                return new MySqlInsertEvent(intermediateEvent);

             case EventType.UPDATE:
                return new MySqlUpdateEvent(intermediateEvent);

             case EventType.DELETE:
                return new MySqlDeleteEvent(intermediateEvent);

        }

        return null; // TODO correcte error handling
    }
}

