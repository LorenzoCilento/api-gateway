using ApiGateway.Metrics;
using System.Diagnostics;

namespace ApiGateway.Middleware;

/// <summary>
/// Middleware per raccogliere metriche Prometheus
/// </summary>
public class MetricsMiddleware
{
    private readonly RequestDelegate _next;

    public MetricsMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "unknown";
        var method = context.Request.Method;

        // Incrementa richieste in corso
        GatewayMetrics.RequestsInProgress.Inc();

        // Misura durata
        var stopwatch = Stopwatch.StartNew();

        try
        {
            await _next(context);

            stopwatch.Stop();

            // Registra metriche successo
            var statusCode = context.Response.StatusCode.ToString();
            
            GatewayMetrics.RequestsTotal
                .WithLabels(method, path, statusCode)
                .Inc();

            GatewayMetrics.RequestDuration
                .WithLabels(method, path)
                .Observe(stopwatch.Elapsed.TotalSeconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            // Registra errore
            GatewayMetrics.ErrorsTotal
                .WithLabels(ex.GetType().Name, path)
                .Inc();

            GatewayMetrics.RequestsTotal
                .WithLabels(method, path, "500")
                .Inc();

            throw;
        }
        finally
        {
            // Decrementa richieste in corso
            GatewayMetrics.RequestsInProgress.Dec();
        }
    }
}
