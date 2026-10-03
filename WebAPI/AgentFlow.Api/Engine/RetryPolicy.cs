namespace AgentFlow.Api.Engine;

public static class RetryPolicy
{
    public static async Task<T> RunAsync<T>(
        Func<int, Task<T>> action,
        int maxAttempts,
        int baseDelayMs,
        Action<int, Exception> onRetry,
        CancellationToken ct)
    {
        maxAttempts = Math.Max(1, maxAttempts);
        Exception? last = null;

        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            ct.ThrowIfCancellationRequested();
            try { return await action(attempt); }
            catch (Exception ex)
            {
                last = ex;
                if (attempt == maxAttempts) break;
                onRetry(attempt, ex);
                var delay = baseDelayMs * (int)Math.Pow(2, attempt - 1);
                await Task.Delay(delay, ct);
            }
        }
        throw last!;
    }
}