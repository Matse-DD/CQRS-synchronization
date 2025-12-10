using Application.Contracts.Events.EventOptions;
using Application.Contracts.Observer;

namespace ApplicationTests.Shared;

public class MockObserver(IEnumerable<string>? events) : IObserver
{
    private readonly List<string> _events = events?.ToList() ?? [];

    public async void StartListening(Action<string> callback)
    {
        _events.ForEach(callback);
    }

    public void StopListening()
    {

    }
}