using Application.Contracts.Persistence;

namespace ApplicationTests.Shared.Persistence;

// Ok dit is ook redelijk hacky maar het is beter dan Thread.Sleep()
public class SynchronizedQueryRepository(int expectedCount) : IQueryRepository {
    private readonly List<string> _history = new();
    private readonly TaskCompletionSource _completionSource = new();

    public IReadOnlyList<string> History => _history;

    public Task Execute(string command, Guid eventId)
    {
        lock (_history)
        {
            _history.Add(command);
            
            if (_history.Count >= expectedCount)
            {
                _completionSource.TrySetResult();
            }
        }
        return Task.CompletedTask;
    }

    public Task<Guid> GetLastSuccessfulEventId()
    {
        return Task.FromResult(Guid.Empty);
    }
    
    public Task WaitForCompletionAsync()
    {
        return _completionSource.Task;
    }
}