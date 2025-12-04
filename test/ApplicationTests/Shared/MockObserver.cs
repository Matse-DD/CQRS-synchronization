using Application.Contracts.Events.EventOptions;
using Application.Contracts.Observer;

namespace ApplicationTests.Shared;

public class MockObserver(IEnumerable<Event>? events) : IObserver
{
    private List<Event> _events = events?.ToList() ?? [];

    public Task StartListening(Action<Event> callback)
    {
        _events.ForEach(callback);
        return Task.CompletedTask;
    }
}