using Application.Contracts.Persistence;

namespace ApplicationTests.Shared.Persistence;

// Ok dit is ook redelijk hacky maar het is beter dan Thread.Sleep()
public class SynchronizedQueryRepository(int expectedCount) : IQueryRepository
{
    private readonly List<string> _history = new();
    private readonly TaskCompletionSource _completionSource = new();

    public IReadOnlyList<string> History => _history;

    public Task Execute(object command, Guid eventId) // TODO PAS OP STRING COMMAND
    {
        lock (_history)
        {
            _history.Add((string) command); // TODO PAS OP EXPCILIT CAST DIT IS ENKEL VOOR TEST TE KUNNEN PROBEREN

            if (_history.Count >= expectedCount)
            {
                _completionSource.TrySetResult();
            }
        }
        return Task.CompletedTask;
    }

    public Task Clear()
    {
        _history.Clear();
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