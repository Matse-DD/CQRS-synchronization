namespace Application.Contracts.Observer;

public interface IObserver
{
    Task StartListening(Action<string> callback, CancellationToken cancellationToken);
    void StopListening(); // Geen idee of dit nog nodig is als je met token werkt (gemakkelijker)
}