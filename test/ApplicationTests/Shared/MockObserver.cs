using Application.Contracts.Events.EventOptions;
using Application.Contracts.Observer;
using Application.Contracts.Persistence;

namespace ApplicationTests.Shared;

public class MockObserver(IEnumerable<string>? events) : IObserver
{
    private List<string> _events = events?.ToList() ?? [];

    public Task StartListening(Action<string> callback)
    {
        _events.ForEach(callback);
        return Task.CompletedTask;
    }
}