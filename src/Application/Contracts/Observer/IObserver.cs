using Application.Contracts.Events.EventOptions;

namespace Application.Contracts.Observer;

public interface IObserver
{
    Task StartListening(Action<Event> callback);
}