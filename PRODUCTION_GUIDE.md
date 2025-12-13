# 🚀 Production Deployment Guide

Guida completa per deployment in produzione dell'API Gateway.

---

## ✅ Checklist Pre-Production

### Security
- [ ] HTTPS abilitato (KEYCLOAK_REQUIRE_HTTPS=true)
- [ ] Security headers configurati
- [ ] Rate limiting configurato appropriatamente
- [ ] CORS limitato ai domini production
- [ ] Secrets management (Kubernetes/Vault)
- [ ] API keys rotate regolarmente

### Monitoring
- [ ] Prometheus scraping configurato
- [ ] Grafana dashboards importate
- [ ] Alerting configurato (Alertmanager)
- [ ] Log centralization (Seq/ELK/CloudWatch)
- [ ] Health checks testati
- [ ] Circuit breaker testato

### Performance
- [ ] Load testing eseguito (k6/JMeter)
- [ ] Timeout configurati
- [ ] Connection pooling ottimizzato
- [ ] Resource limits impostati (CPU/Memory)

### Resilience
- [ ] Circuit breaker configurato
- [ ] Retry policies testate
- [ ] Failover testato
- [ ] Backup e recovery plan

---

## 📊 Prometheus + Grafana Setup

### 1. Prometheus Configuration

```yaml
# infrastructure/prometheus.yml
global:
  scrape_interval: 15s
  evaluation_interval: 15s

scrape_configs:
  - job_name: 'api-gateway'
    static_configs:
      - targets: ['api-gateway:5000']
    metrics_path: '/metrics'
    
  - job_name: 'auth-service'
    static_configs:
      - targets: ['auth-service:5001']
    metrics_path: '/metrics'
```

### 2. Docker Compose Monitoring

```yaml
# infrastructure/docker-compose.monitoring.yml
version: '3.8'

services:
  prometheus:
    image: prom/prometheus:latest
    container_name: prometheus
    ports:
      - "9090:9090"
    volumes:
      - ./prometheus.yml:/etc/prometheus/prometheus.yml
      - prometheus-data:/prometheus
    command:
      - '--config.file=/etc/prometheus/prometheus.yml'
      - '--storage.tsdb.path=/prometheus'
    networks:
      - microservices-network

  grafana:
    image: grafana/grafana:latest
    container_name: grafana
    ports:
      - "3000:3000"
    environment:
      - GF_SECURITY_ADMIN_PASSWORD=admin
      - GF_USERS_ALLOW_SIGN_UP=false
    volumes:
      - grafana-data:/var/lib/grafana
      - ./grafana/dashboards:/etc/grafana/provisioning/dashboards
      - ./grafana/datasources:/etc/grafana/provisioning/datasources
    depends_on:
      - prometheus
    networks:
      - microservices-network

  alertmanager:
    image: prom/alertmanager:latest
    container_name: alertmanager
    ports:
      - "9093:9093"
    volumes:
      - ./alertmanager.yml:/etc/alertmanager/alertmanager.yml
    networks:
      - microservices-network

volumes:
  prometheus-data:
  grafana-data:

networks:
  microservices-network:
    external: true
```

### 3. Avvia Stack Monitoring

```bash
cd infrastructure
docker-compose -f docker-compose.monitoring.yml up -d

# Accedi a:
# Prometheus: http://localhost:9090
# Grafana: http://localhost:3000 (admin/admin)
```

---

## 📈 Metrics Available

### Gateway Metrics

```promql
# Request rate
rate(gateway_requests_total[5m])

# Request duration (p95)
histogram_quantile(0.95, rate(gateway_request_duration_seconds_bucket[5m]))

# Error rate
rate(gateway_errors_total[5m])

# Requests in progress
gateway_requests_in_progress

# Circuit breaker state
gateway_circuit_breaker_state
```

### Upstream Metrics

```promql
# Upstream request rate
rate(gateway_upstream_requests_total[5m])

# Upstream latency
histogram_quantile(0.99, rate(gateway_upstream_request_duration_seconds_bucket[5m]))
```

### HTTP Metrics (automatic)

```promql
# HTTP request duration
http_request_duration_seconds

# HTTP requests in progress
http_requests_in_progress
```

---

## 🎯 Grafana Dashboards

### Import Dashboard

1. Open Grafana → Dashboards → Import
2. Upload `grafana/dashboards/api-gateway.json`

### Key Panels

**Request Rate**
```promql
sum(rate(gateway_requests_total[5m])) by (route)
```

**Latency Percentiles**
```promql
histogram_quantile(0.50, rate(gateway_request_duration_seconds_bucket[5m]))
histogram_quantile(0.95, rate(gateway_request_duration_seconds_bucket[5m]))
histogram_quantile(0.99, rate(gateway_request_duration_seconds_bucket[5m]))
```

**Error Rate**
```promql
sum(rate(gateway_errors_total[5m])) by (error_type)
```

**Circuit Breaker**
```promql
gateway_circuit_breaker_state
```

---

## 🚨 Alerting Rules

### Prometheus Alerts

```yaml
# infrastructure/alerts.yml
groups:
  - name: api_gateway_alerts
    interval: 30s
    rules:
      # High error rate
      - alert: HighErrorRate
        expr: |
          rate(gateway_errors_total[5m]) > 0.05
        for: 5m
        labels:
          severity: warning
        annotations:
          summary: "High error rate on API Gateway"
          description: "Error rate is {{ $value }} errors/sec"

      # High latency
      - alert: HighLatency
        expr: |
          histogram_quantile(0.95, rate(gateway_request_duration_seconds_bucket[5m])) > 2
        for: 5m
        labels:
          severity: warning
        annotations:
          summary: "High latency on API Gateway"
          description: "P95 latency is {{ $value }}s"

      # Circuit breaker open
      - alert: CircuitBreakerOpen
        expr: |
          gateway_circuit_breaker_state > 0
        for: 1m
        labels:
          severity: critical
        annotations:
          summary: "Circuit breaker open"
          description: "Circuit breaker for {{ $labels.service }} is open"

      # Service down
      - alert: ServiceDown
        expr: |
          up{job="api-gateway"} == 0
        for: 1m
        labels:
          severity: critical
        annotations:
          summary: "API Gateway is down"
```

### Alertmanager Configuration

```yaml
# infrastructure/alertmanager.yml
global:
  resolve_timeout: 5m

route:
  group_by: ['alertname', 'severity']
  group_wait: 10s
  group_interval: 10s
  repeat_interval: 12h
  receiver: 'team-notifications'

receivers:
  - name: 'team-notifications'
    email_configs:
      - to: 'team@yourdomain.com'
        from: 'alertmanager@yourdomain.com'
        smarthost: 'smtp.gmail.com:587'
        auth_username: 'your-email@gmail.com'
        auth_password: 'your-app-password'
    
    slack_configs:
      - api_url: 'https://hooks.slack.com/services/YOUR/SLACK/WEBHOOK'
        channel: '#alerts'
        title: '{{ .GroupLabels.alertname }}'
        text: '{{ range .Alerts }}{{ .Annotations.description }}{{ end }}'
```

---

## ☸️ Kubernetes Deployment

### 1. Deployment YAML

```yaml
# k8s/deployment.yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: api-gateway
  labels:
    app: api-gateway
spec:
  replicas: 3
  selector:
    matchLabels:
      app: api-gateway
  template:
    metadata:
      labels:
        app: api-gateway
      annotations:
        prometheus.io/scrape: "true"
        prometheus.io/port: "5000"
        prometheus.io/path: "/metrics"
    spec:
      containers:
      - name: api-gateway
        image: your-registry/api-gateway:latest
        ports:
        - containerPort: 5000
          name: http
        env:
        - name: ASPNETCORE_ENVIRONMENT
          value: "Production"
        - name: KEYCLOAK_AUTHORITY
          valueFrom:
            secretKeyRef:
              name: gateway-secrets
              key: keycloak-authority
        - name: AUTH_SERVICE_URL
          value: "http://auth-service:5001"
        resources:
          requests:
            memory: "256Mi"
            cpu: "250m"
          limits:
            memory: "512Mi"
            cpu: "500m"
        livenessProbe:
          httpGet:
            path: /health
            port: 5000
          initialDelaySeconds: 30
          periodSeconds: 10
        readinessProbe:
          httpGet:
            path: /health
            port: 5000
          initialDelaySeconds: 5
          periodSeconds: 5
---
apiVersion: v1
kind: Service
metadata:
  name: api-gateway
spec:
  selector:
    app: api-gateway
  ports:
  - protocol: TCP
    port: 80
    targetPort: 5000
  type: LoadBalancer
---
apiVersion: autoscaling/v2
kind: HorizontalPodAutoscaler
metadata:
  name: api-gateway-hpa
spec:
  scaleTargetRef:
    apiVersion: apps/v1
    kind: Deployment
    name: api-gateway
  minReplicas: 3
  maxReplicas: 10
  metrics:
  - type: Resource
    resource:
      name: cpu
      target:
        type: Utilization
        averageUtilization: 70
  - type: Resource
    resource:
      name: memory
      target:
        type: Utilization
        averageUtilization: 80
```

### 2. ConfigMap

```yaml
# k8s/configmap.yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: gateway-config
data:
  appsettings.Production.json: |
    {
      "Keycloak": {
        "RequireHttpsMetadata": true
      },
      "Cors": {
        "AllowedOrigins": [
          "https://app.yourdomain.com",
          "https://admin.yourdomain.com"
        ]
      }
    }
```

### 3. Secrets

```bash
# Create secrets
kubectl create secret generic gateway-secrets \
  --from-literal=keycloak-authority=https://keycloak.yourdomain.com/realms/prod \
  --from-literal=seq-api-key=your-seq-key
```

### 4. Deploy

```bash
kubectl apply -f k8s/
kubectl get pods -l app=api-gateway
kubectl logs -f deployment/api-gateway
```

---

## 🧪 Load Testing

### K6 Load Test

```javascript
// load-test.js
import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
  stages: [
    { duration: '2m', target: 100 },  // Ramp-up
    { duration: '5m', target: 100 },  // Stay at 100
    { duration: '2m', target: 200 },  // Ramp-up
    { duration: '5m', target: 200 },  // Stay at 200
    { duration: '2m', target: 0 },    // Ramp-down
  ],
  thresholds: {
    http_req_duration: ['p(95)<500'], // 95% under 500ms
    http_req_failed: ['rate<0.01'],   // Error rate < 1%
  },
};

export default function () {
  const res = http.get('http://localhost:5000/api/gateway/info');
  
  check(res, {
    'status is 200': (r) => r.status === 200,
    'response time < 500ms': (r) => r.timings.duration < 500,
  });

  sleep(1);
}
```

### Run Load Test

```bash
k6 run load-test.js

# With cloud reporting
k6 cloud load-test.js
```

---

## 🔐 Security Hardening

### 1. TLS/HTTPS

```yaml
# In production, always use TLS
environment:
  - ASPNETCORE_URLS=https://+:5000
  - ASPNETCORE_Kestrel__Certificates__Default__Path=/certs/cert.pfx
  - ASPNETCORE_Kestrel__Certificates__Default__Password=your-cert-password
```

### 2. API Gateway Firewall

```bash
# Allow only specific IPs/ranges
# Use cloud provider firewall (AWS Security Groups, Azure NSG, etc.)
```

### 3. Secrets Management

**Azure Key Vault:**
```csharp
builder.Configuration.AddAzureKeyVault(
    new Uri($"https://{keyVaultName}.vault.azure.net/"),
    new DefaultAzureCredential());
```

**AWS Secrets Manager:**
```csharp
builder.Configuration.AddSecretsManager();
```

---

## 📊 Cost Optimization

### Resource Limits

```yaml
resources:
  requests:
    memory: "256Mi"
    cpu: "250m"
  limits:
    memory: "512Mi"
    cpu: "500m"
```

### Auto-scaling

- Min replicas: 2 (HA)
- Max replicas: 10
- Scale on: CPU 70%, Memory 80%

---

## 🆘 Incident Response

### Runbook

1. **Check metrics**: Grafana dashboard
2. **Check logs**: Seq/CloudWatch
3. **Check health**: `/health` endpoint
4. **Scale if needed**: `kubectl scale deployment api-gateway --replicas=5`
5. **Restart if needed**: `kubectl rollout restart deployment/api-gateway`

---

**🎯 Remember:** Monitor, Alert, Iterate!
