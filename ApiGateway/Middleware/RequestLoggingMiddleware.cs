namespace ApiGateway.Middleware;

/// <summary>
/// Middleware per il logging strutturato delle richieste HTTP
/// </summary>
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var startTime = DateTime.UtcNow;
        var correlationId = context.Items["CorrelationId"]?.ToString() ?? "N/A";

        try
        {
            _logger.LogInformation(
                "Incoming request: {Method} {Path} from {RemoteIp} - CorrelationId: {CorrelationId}",
                context.Request.Method,
                context.Request.Path,
                context.Connection.RemoteIpAddress,
                correlationId);

            await _next(context);

            var duration = DateTime.UtcNow - startTime;
            
            _logger.LogInformation(
                "Completed request: {Method} {Path} - Status: {StatusCode} - Duration: {Duration}ms - CorrelationId: {CorrelationId}",
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode,
                duration.TotalMilliseconds,
                correlationId);
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;
            
            _logger.LogError(ex,
                "Request failed: {Method} {Path} - Duration: {Duration}ms - CorrelationId: {CorrelationId}",
                context.Request.Method,
                context.Request.Path,
                duration.TotalMilliseconds,
                correlationId);

            throw;
        }
    }
}
