using Application.Contracts.Events.EventOptions;

namespace Application.Contracts.Events.Factory;

public interface IEventFactory
{
    public Event DetermineEvent(string incomingEvent);
}
