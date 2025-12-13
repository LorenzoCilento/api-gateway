using ApiGateway.Middleware;
using ApiGateway.HealthChecks;
using ApiGateway.Resilience;
using ApiGateway.Metrics;
using AspNetCoreRateLimit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Prometheus;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;

// Configure Serilog with structured logging for microservices
var loggerConfig = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
    .MinimumLevel.Override("Yarp", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithEnvironmentName()
    .Enrich.WithThreadId()
    .Enrich.WithProperty("Application", "ApiGateway")
    .Enrich.WithProperty("ServiceName", "api-gateway")
    .Enrich.WithProperty("ServiceVersion", "1.0.0");

// Console output - JSON structured for production, human-readable for development
var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
if (environment == "Development")
{
    loggerConfig.WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{ServiceName}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}");
}
else
{
    // JSON structured logging for production (ELK, Splunk, etc.)
    loggerConfig.WriteTo.Console(new CompactJsonFormatter());
}

// File output - Always JSON for easy parsing
loggerConfig.WriteTo.File(
    formatter: new CompactJsonFormatter(),
    path: "logs/api-gateway-.json",
    rollingInterval: RollingInterval.Day,
    retainedFileCountLimit: 30,
    fileSizeLimitBytes: 10485760, // 10MB
    rollOnFileSizeLimit: true);

// Optional: Seq for centralized logging (configure SEQ_URL environment variable)
var seqUrl = Environment.GetEnvironmentVariable("SEQ_URL");
if (!string.IsNullOrEmpty(seqUrl))
{
    var seqApiKey = Environment.GetEnvironmentVariable("SEQ_API_KEY");
    loggerConfig.WriteTo.Seq(seqUrl, apiKey: seqApiKey);
    Console.WriteLine($"Seq logging enabled: {seqUrl}");
}

Log.Logger = loggerConfig.CreateLogger();

try
{
    Log.Information("Starting API Gateway");

    var builder = WebApplication.CreateBuilder(args);

    // Add Serilog
    builder.Host.UseSerilog();

    // Add HttpContextAccessor for Correlation ID
    builder.Services.AddHttpContextAccessor();

    // Configure Rate Limiting
    builder.Services.AddMemoryCache();
    builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
    builder.Services.Configure<IpRateLimitPolicies>(builder.Configuration.GetSection("IpRateLimitPolicies"));
    builder.Services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
    builder.Services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
    builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
    builder.Services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();

    // Configure JWT Authentication
    var keycloakAuthority = builder.Configuration["Keycloak:Authority"];
    var requireHttpsMetadata = builder.Configuration.GetValue<bool>("Keycloak:RequireHttpsMetadata");

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.Authority = keycloakAuthority;
            options.RequireHttpsMetadata = requireHttpsMetadata;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = false,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ClockSkew = TimeSpan.FromSeconds(30)
            };

            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    // Support JWT from cookie (HttpOnly) or Authorization header
                    if (context.Request.Cookies.TryGetValue("access_token", out var cookieToken))
                    {
                        context.Token = cookieToken;
                    }
                    return Task.CompletedTask;
                },
                OnAuthenticationFailed = context =>
                {
                    Log.Warning("JWT Authentication failed: {Error}", context.Exception.Message);
                    return Task.CompletedTask;
                },
                OnTokenValidated = context =>
                {
                    var userId = context.Principal?.FindFirst("sub")?.Value;
                    Log.Information("JWT Token validated for user: {UserId}", userId);
                    return Task.CompletedTask;
                }
            };
        });

    builder.Services.AddAuthorization();

    // Configure CORS
    var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowFrontends", policy =>
        {
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials(); // Important for HttpOnly cookies
        });
    });

    // Add YARP Reverse Proxy
    builder.Services.AddReverseProxy()
        .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
        .AddTransforms(builderContext =>
        {
            // Add correlation ID to all proxied requests
            builderContext.AddRequestTransform(async transformContext =>
            {
                var httpContext = transformContext.HttpContext;
                if (httpContext.Items.TryGetValue("CorrelationId", out var correlationId))
                {
                    transformContext.ProxyRequest.Headers.Add("X-Correlation-ID", correlationId?.ToString());
                }
                await Task.CompletedTask;
            });

            // Collect upstream metrics
            builderContext.AddResponseTransform(async transformContext =>
            {
                var httpContext = transformContext.HttpContext;
                var route = httpContext.GetRouteValue("routeId")?.ToString() ?? "unknown";
                var statusCode = transformContext.ProxyResponse?.StatusCode.ToString() ?? "unknown";
                
                GatewayMetrics.UpstreamRequestsTotal
                    .WithLabels(route, statusCode)
                    .Inc();
                
                await Task.CompletedTask;
            });
        });

    // Add Health Checks
    builder.Services.AddHealthChecks()
        .AddCheck<GatewayHealthCheck>("gateway_health")
        .AddUrlGroup(new Uri($"{builder.Configuration["ReverseProxy:Clusters:auth-cluster:Destinations:auth-service:Address"]}/health"), 
            name: "auth-service", 
            timeout: TimeSpan.FromSeconds(5));

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    var app = builder.Build();

    // Configure middleware pipeline
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    // Security Headers
    app.Use(async (context, next) =>
    {
        context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
        context.Response.Headers.Append("X-Frame-Options", "DENY");
        context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
        context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
        await next();
    });

    app.UseIpRateLimiting();

    // Custom Middleware
    app.UseMiddleware<CorrelationIdMiddleware>();
    app.UseMiddleware<RequestLoggingMiddleware>();
    app.UseMiddleware<MetricsMiddleware>();

    app.UseCors("AllowFrontends");

    app.UseAuthentication();
    app.UseAuthorization();

    // Prometheus metrics endpoint
    app.UseHttpMetrics(); // Metriche HTTP automatiche
    app.MapMetrics();     // Endpoint /metrics

    app.MapHealthChecks("/health");
    app.MapControllers();

    // Map YARP routes
    app.MapReverseProxy();

    app.Run();

    Log.Information("API Gateway started successfully");
}
catch (Exception ex)
{
    Log.Fatal(ex, "API Gateway failed to start");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
