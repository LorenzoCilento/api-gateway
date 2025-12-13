using Microsoft.AspNetCore.Mvc;

namespace ApiGateway.Controllers;

/// <summary>
/// Debug controller - disponibile solo in Development
/// In produzione l'API Gateway dovrebbe essere trasparente (solo proxy)
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class GatewayController : ControllerBase
{
    private readonly ILogger<GatewayController> _logger;
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;

    public GatewayController(
        ILogger<GatewayController> logger, 
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        _logger = logger;
        _configuration = configuration;
        _environment = environment;
    }

    /// <summary>
    /// Get Gateway information (solo Development)
    /// </summary>
    [HttpGet("info")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetInfo()
    {
        // Disponibile solo in Development
        if (!_environment.IsDevelopment())
        {
            return NotFound();
        }

        var info = new
        {
            Service = "API Gateway",
            Version = _configuration["Gateway:Version"] ?? "1.0.0",
            Framework = "YARP",
            Runtime = Environment.Version.ToString(),
            Environment = _environment.EnvironmentName,
            MachineName = Environment.MachineName,
            Timestamp = DateTime.UtcNow
        };

        return Ok(info);
    }
}
