namespace IntegrationTests.Helpers;

public static class TestHelpers
{

    public static async Task AssertEventuallyAsync(Func<Task<bool>> condition, int timeoutMs = 5000, int intervalMs = 100)
    {
        DateTime start = DateTime.UtcNow;
        while ((DateTime.UtcNow - start).TotalMilliseconds < timeoutMs)
        {
            if (await condition())
            {
                return;
            }
            await Task.Delay(intervalMs);
        }

        throw new TimeoutException($"Condition was not met within {timeoutMs}ms");
    }

    public static async Task AssertEventuallyAsync(Func<bool> condition, int timeoutMs = 5000, int intervalMs = 100)
    {
        await AssertEventuallyAsync(() => Task.FromResult(condition()), timeoutMs, intervalMs);
    }
}
