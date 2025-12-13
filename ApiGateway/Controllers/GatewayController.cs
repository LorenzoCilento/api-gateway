using Microsoft.AspNetCore.Mvc;

namespace ApiGateway.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GatewayController : ControllerBase
{
    private readonly ILogger<GatewayController> _logger;

    public GatewayController(ILogger<GatewayController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Get Gateway information
    /// </summary>
    [HttpGet("info")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetInfo()
    {
        var info = new
        {
            Service = "API Gateway",
            Version = "1.0.0",
            Framework = "YARP (Yet Another Reverse Proxy)",
            Runtime = Environment.Version.ToString(),
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production",
            MachineName = Environment.MachineName,
            Timestamp = DateTime.UtcNow
        };

        return Ok(info);
    }

    /// <summary>
    /// Get routes configuration (for debugging)
    /// </summary>
    [HttpGet("routes")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetRoutes()
    {
        var routes = new
        {
            Routes = new[]
            {
                new { Path = "/api/auth/**", Target = "Auth Service", Cluster = "auth-cluster" }
            }
        };

        return Ok(routes);
    }
}
