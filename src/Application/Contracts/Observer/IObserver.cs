namespace Application.Contracts.Observer;

public interface IObserver
{
    Task StartListening(Action<string> callback, CancellationToken cancellationToken);
}