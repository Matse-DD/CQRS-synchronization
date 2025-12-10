namespace Application.Contracts.Observer;

public interface IObserver
{
    void StartListening(Action<string> callback);
    void StopListening();
}