using System.Net;
using NUnit.Framework;

namespace Chatbot.Tests.Gds;

internal static class GdsLlmRetry
{
    private static readonly HttpStatusCode[] RetriableStatusCodes =
    [
        HttpStatusCode.TooManyRequests,
        HttpStatusCode.ServiceUnavailable,
        HttpStatusCode.GatewayTimeout,
    ];

    public static async Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> action,
        int maxAttempts,
        CancellationToken cancellationToken = default)
    {
        if (maxAttempts < 1)
            throw new ArgumentOutOfRangeException(nameof(maxAttempts));

        Exception? lastException = null;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                return await action(cancellationToken).ConfigureAwait(false);
            }
            catch (HttpRequestException ex) when (IsRetriable(ex) && attempt < maxAttempts)
            {
                lastException = ex;
                var delay = ComputeDelay(attempt, ex);
                TestContext.WriteLine(
                    $"GDS LLM retry {attempt}/{maxAttempts} after {(int)ex.StatusCode!.Value} — waiting {delay.TotalSeconds:F0}s.");
                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }
        }

        throw lastException ?? new InvalidOperationException("GDS LLM retry failed without capturing an exception.");
    }

    private static bool IsRetriable(HttpRequestException ex) =>
        ex.StatusCode is { } status && RetriableStatusCodes.Contains(status);

    private static TimeSpan ComputeDelay(int attempt, HttpRequestException ex)
    {
        if (ex.StatusCode == HttpStatusCode.TooManyRequests)
            return TimeSpan.FromSeconds(Math.Min(90, 20 * attempt));

        return TimeSpan.FromSeconds(Math.Min(30, Math.Pow(2, attempt)));
    }
}
