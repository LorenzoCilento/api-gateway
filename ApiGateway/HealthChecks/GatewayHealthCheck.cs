using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ApiGateway.HealthChecks;

/// <summary>
/// Health check per verificare lo stato generale dell'API Gateway
/// </summary>
public class GatewayHealthCheck : IHealthCheck
{
    private readonly ILogger<GatewayHealthCheck> _logger;

    public GatewayHealthCheck(ILogger<GatewayHealthCheck> logger)
    {
        _logger = logger;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Check basic gateway health (memory, etc.)
            var memoryUsed = GC.GetTotalMemory(false) / 1024 / 1024; // MB
            
            if (memoryUsed > 500) // Warning if over 500MB
            {
                _logger.LogWarning("High memory usage: {MemoryUsed}MB", memoryUsed);
                return Task.FromResult(
                    HealthCheckResult.Degraded($"High memory usage: {memoryUsed}MB"));
            }

            return Task.FromResult(
                HealthCheckResult.Healthy($"Gateway is healthy. Memory: {memoryUsed}MB"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed");
            return Task.FromResult(
                HealthCheckResult.Unhealthy("Gateway health check failed", ex));
        }
    }
}
