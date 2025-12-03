using Application.Contracts.Events;
using Application.Contracts.Ports;

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