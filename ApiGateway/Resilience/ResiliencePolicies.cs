using Polly;
using Polly.Extensions.Http;

namespace ApiGateway.Resilience;

/// <summary>
/// Resilience policies per proteggere il gateway da fallimenti dei servizi downstream
/// </summary>
public static class ResiliencePolicies
{
    /// <summary>
    /// Circuit Breaker Policy: interrompe le chiamate a servizi non disponibili
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => (int)msg.StatusCode >= 500)
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 5,  // 5 fallimenti consecutivi
                durationOfBreak: TimeSpan.FromSeconds(30), // Apri per 30 secondi
                onBreak: (outcome, duration) =>
                {
                    Serilog.Log.Warning(
                        "Circuit breaker opened for {Duration}s due to {Exception}",
                        duration.TotalSeconds,
                        outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString());
                },
                onReset: () =>
                {
                    Serilog.Log.Information("Circuit breaker reset - service recovered");
                },
                onHalfOpen: () =>
                {
                    Serilog.Log.Information("Circuit breaker half-open - testing service");
                });
    }

    /// <summary>
    /// Retry Policy: riprova automaticamente in caso di errori transitori
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => (int)msg.StatusCode == 429) // Too Many Requests
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), // Exponential backoff
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    Serilog.Log.Warning(
                        "Retry {RetryCount} after {Delay}s due to {Reason}",
                        retryCount,
                        timespan.TotalSeconds,
                        outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString());
                });
    }

    /// <summary>
    /// Timeout Policy: limita il tempo di attesa per le risposte
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> GetTimeoutPolicy()
    {
        return Policy.TimeoutAsync<HttpResponseMessage>(
            timeout: TimeSpan.FromSeconds(30),
            onTimeoutAsync: (context, timespan, task) =>
            {
                Serilog.Log.Warning(
                    "Request timed out after {Timeout}s",
                    timespan.TotalSeconds);
                return Task.CompletedTask;
            });
    }

    /// <summary>
    /// Policy combinata: Timeout → Retry → Circuit Breaker
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> GetCombinedPolicy()
    {
        return Policy.WrapAsync(
            GetCircuitBreakerPolicy(),
            GetRetryPolicy(),
            GetTimeoutPolicy());
    }
}
