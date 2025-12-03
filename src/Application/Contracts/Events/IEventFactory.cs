namespace Application.Contracts.Events;

public interface IEventFactory
{
    public Event DetermineEvent(string incomingEvent);
}
