using Application.Contracts.Events;

namespace Application.Contracts.Ports;

public interface IObserver
{
    Task StartListening(Action<Event> callback);
}