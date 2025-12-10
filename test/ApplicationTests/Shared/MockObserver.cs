using Application.Contracts.Observer;

namespace ApplicationTests.Shared;

public class MockObserver(IEnumerable<string>? events) : IObserver
{
    private readonly List<string> _events = events?.ToList() ?? [];

    public Task StartListening(Action<string> callback, CancellationToken cancellationToken)
    {
        _events.ForEach(callback);
        return Task.CompletedTask;
    }
}