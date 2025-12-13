namespace ApiGateway.Middleware;

/// <summary>
/// Middleware che gestisce il Correlation ID per tracciare le richieste attraverso i microservizi
/// </summary>
public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private const string CorrelationIdHeader = "X-Correlation-ID";

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        string correlationId;

        // Check if correlation ID exists in request header
        if (context.Request.Headers.TryGetValue(CorrelationIdHeader, out var existingCorrelationId))
        {
            correlationId = existingCorrelationId.ToString();
        }
        else
        {
            // Generate new correlation ID
            correlationId = Guid.NewGuid().ToString();
        }

        // Store in HttpContext for use in other middleware
        context.Items["CorrelationId"] = correlationId;

        // Add to response headers
        context.Response.Headers.Append(CorrelationIdHeader, correlationId);

        // Add to logging context
        using (Serilog.Context.LogContext.PushProperty("CorrelationId", correlationId))
        {
            await _next(context);
        }
    }
}
