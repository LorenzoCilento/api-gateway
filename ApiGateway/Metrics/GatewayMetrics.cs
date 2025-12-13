using Prometheus;

namespace ApiGateway.Metrics;

/// <summary>
/// Custom metrics per monitoraggio dell'API Gateway
/// </summary>
public static class GatewayMetrics
{
    // Counter per richieste totali
    public static readonly Counter RequestsTotal = Prometheus.Metrics.CreateCounter(
        "gateway_requests_total",
        "Total number of requests processed by the gateway",
        new CounterConfiguration
        {
            LabelNames = new[] { "method", "route", "status_code" }
        });

    // Histogram per durata richieste
    public static readonly Histogram RequestDuration = Prometheus.Metrics.CreateHistogram(
        "gateway_request_duration_seconds",
        "Duration of HTTP requests in seconds",
        new HistogramConfiguration
        {
            LabelNames = new[] { "method", "route" },
            Buckets = Histogram.ExponentialBuckets(0.001, 2, 15) // 1ms to ~32s
        });

    // Gauge per richieste correnti
    public static readonly Gauge RequestsInProgress = Prometheus.Metrics.CreateGauge(
        "gateway_requests_in_progress",
        "Number of requests currently being processed");

    // Counter per errori
    public static readonly Counter ErrorsTotal = Prometheus.Metrics.CreateCounter(
        "gateway_errors_total",
        "Total number of errors",
        new CounterConfiguration
        {
            LabelNames = new[] { "error_type", "route" }
        });

    // Gauge per circuit breaker state
    public static readonly Gauge CircuitBreakerState = Prometheus.Metrics.CreateGauge(
        "gateway_circuit_breaker_state",
        "Circuit breaker state (0=Closed, 1=Open, 2=HalfOpen)",
        new GaugeConfiguration
        {
            LabelNames = new[] { "service" }
        });

    // Counter per upstream service calls
    public static readonly Counter UpstreamRequestsTotal = Prometheus.Metrics.CreateCounter(
        "gateway_upstream_requests_total",
        "Total requests to upstream services",
        new CounterConfiguration
        {
            LabelNames = new[] { "service", "status_code" }
        });

    // Histogram per upstream response time
    public static readonly Histogram UpstreamRequestDuration = Prometheus.Metrics.CreateHistogram(
        "gateway_upstream_request_duration_seconds",
        "Duration of upstream requests in seconds",
        new HistogramConfiguration
        {
            LabelNames = new[] { "service" },
            Buckets = Histogram.ExponentialBuckets(0.01, 2, 12) // 10ms to ~40s
        });
}
