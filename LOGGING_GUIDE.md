# 📊 Logging & Monitoring Guide

Guida completa al sistema di logging strutturato per architettura a microservizi.

---

## 🎯 Architettura Logging

```
┌─────────────┐     ┌─────────────┐     ┌─────────────┐
│ API Gateway │────▶│     Seq     │◀────│Auth Service │
│  (JSON)     │     │   (Central  │     │   (JSON)    │
└─────────────┘     │   Logging)  │     └─────────────┘
                    └──────┬──────┘
                           │
                    ┌──────▼──────┐
                    │ Elasticsearch│
                    │   Kibana     │
                    └──────────────┘
```

---

## ✨ Caratteristiche Implementate

### 1. **Structured Logging (JSON)**
Tutti i log sono in formato JSON per:
- ✅ Parsing automatico da aggregatori (ELK, Splunk, Seq)
- ✅ Ricerca avanzata per campi strutturati
- ✅ Analisi e dashboard automatiche

### 2. **Correlation ID**
- ✅ Tracciamento end-to-end tra microservizi
- ✅ Propagazione automatica via `X-Correlation-ID` header
- ✅ Log correlati tra Gateway → Auth Service → Altri servizi

### 3. **Service Metadata**
Ogni log include:
- `ServiceName`: "api-gateway"
- `ServiceVersion`: "1.0.0"
- `Environment`: Development/Production
- `MachineName`: hostname del container
- `ThreadId`: per debugging concurrency

### 4. **Multiple Sinks**
- **Console**: JSON in production, human-readable in development
- **File**: JSON rolling files (10MB max, 30 giorni retention)
- **Seq** (opzionale): Centralized structured logs
- **Elasticsearch** (configurabile): ELK stack integration

---

## 📝 Formato Log

### Development (Human-Readable)
```
[12:34:56 INF] [api-gateway] [abc-123-def] Incoming request: POST /api/auth/login
[12:34:56 INF] [api-gateway] [abc-123-def] Completed request - Status: 200 - Duration: 145ms
```

### Production (JSON)
```json
{
  "@t": "2025-12-13T12:34:56.789Z",
  "@mt": "Incoming request: {Method} {Path}",
  "Method": "POST",
  "Path": "/api/auth/login",
  "CorrelationId": "abc-123-def-456",
  "ServiceName": "api-gateway",
  "ServiceVersion": "1.0.0",
  "Environment": "Production",
  "MachineName": "gateway-pod-xyz",
  "ThreadId": 42,
  "Level": "Information"
}
```

---

## 🚀 Setup Centralized Logging

### Opzione 1: Seq (Consigliata per sviluppo)

#### 1. Avvia Seq con Docker

```bash
docker run -d \
  --name seq \
  --network microservices-network \
  -p 5341:80 \
  -e ACCEPT_EULA=Y \
  datalust/seq:latest
```

#### 2. Configura Gateway

```bash
# .env file
SEQ_URL=http://seq:5341
SEQ_API_KEY=  # opzionale
```

#### 3. Accedi a Seq

```
http://localhost:5341
```

#### 4. Ricerca Logs per Correlation ID

```sql
CorrelationId = 'abc-123-def'
```

---

### Opzione 2: ELK Stack (Elasticsearch + Logstash + Kibana)

#### 1. Docker Compose per ELK

```yaml
# docker-compose.elk.yml
version: '3.8'

services:
  elasticsearch:
    image: docker.elastic.co/elasticsearch/elasticsearch:8.11.0
    environment:
      - discovery.type=single-node
      - xpack.security.enabled=false
    ports:
      - "9200:9200"
    volumes:
      - elasticsearch-data:/usr/share/elasticsearch/data
    networks:
      - microservices-network

  kibana:
    image: docker.elastic.co/kibana/kibana:8.11.0
    ports:
      - "5601:5601"
    environment:
      - ELASTICSEARCH_HOSTS=http://elasticsearch:9200
    depends_on:
      - elasticsearch
    networks:
      - microservices-network

  logstash:
    image: docker.elastic.co/logstash/logstash:8.11.0
    volumes:
      - ./logstash.conf:/usr/share/logstash/pipeline/logstash.conf
    ports:
      - "5044:5044"
    depends_on:
      - elasticsearch
    networks:
      - microservices-network

volumes:
  elasticsearch-data:

networks:
  microservices-network:
    external: true
```

#### 2. Logstash Configuration

```ruby
# logstash.conf
input {
  tcp {
    port => 5044
    codec => json
  }
}

filter {
  # Parse correlation ID
  if [CorrelationId] {
    mutate {
      add_field => { "trace_id" => "%{CorrelationId}" }
    }
  }
}

output {
  elasticsearch {
    hosts => ["elasticsearch:9200"]
    index => "microservices-%{ServiceName}-%{+YYYY.MM.dd}"
  }
  stdout { codec => rubydebug }
}
```

#### 3. Avvia ELK Stack

```bash
docker-compose -f docker-compose.elk.yml up -d
```

#### 4. Accedi a Kibana

```
http://localhost:5601
```

#### 5. Query in Kibana

```
ServiceName: "api-gateway" AND CorrelationId: "abc-123"
```

---

### Opzione 3: Azure Application Insights

#### 1. Aggiungi Package

```xml
<PackageReference Include="Microsoft.ApplicationInsights.AspNetCore" Version="2.22.0" />
```

#### 2. Configura in Program.cs

```csharp
builder.Services.AddApplicationInsightsTelemetry(
    builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]);
```

#### 3. Environment Variable

```bash
APPLICATIONINSIGHTS_CONNECTION_STRING=InstrumentationKey=xxx;...
```

---

## 🔍 Query Patterns

### Ricerca per Correlation ID

**Seq:**
```sql
CorrelationId = 'abc-123-def'
ORDER BY @t
```

**Kibana:**
```
CorrelationId: "abc-123-def"
```

### Errori per Servizio

**Seq:**
```sql
ServiceName = 'api-gateway' AND Level = 'Error'
WHERE @t >= Now() - 1h
```

**Kibana:**
```
ServiceName: "api-gateway" AND Level: "Error"
```

### Performance Tracking

**Seq:**
```sql
SELECT 
  AVG(Duration) as AvgDuration,
  MAX(Duration) as MaxDuration
FROM stream
WHERE @t >= Now() - 1h
GROUP BY Path
```

### Request Tracing

**Seq:**
```sql
ServiceName IN ['api-gateway', 'auth-service']
  AND CorrelationId = 'abc-123'
ORDER BY @t
```

---

## 📊 Dashboard Setup (Kibana)

### 1. Create Index Pattern

```
Index pattern: microservices-*
Time field: @t
```

### 2. Visualizations

#### Requests per Service
```
Visualization: Pie chart
Metrics: Count
Buckets: Terms - ServiceName
```

#### Response Times
```
Visualization: Line chart
Metrics: Average - Duration
Buckets: Date Histogram - @t
Split Series: ServiceName
```

#### Error Rate
```
Visualization: Area chart
Metrics: Count
Filter: Level = "Error"
Buckets: Date Histogram - @t
```

---

## 🔧 Best Practices

### 1. **Consistent Log Levels**

```csharp
// ✅ GOOD
_logger.LogInformation("User {UserId} logged in successfully", userId);
_logger.LogWarning("Login attempt failed for {Email}", email);
_logger.LogError(ex, "Database connection failed");

// ❌ BAD
_logger.LogInformation($"User {userId} logged in"); // String interpolation
_logger.LogError("An error occurred"); // No context
```

### 2. **Structured Properties**

```csharp
// ✅ GOOD - Structured
_logger.LogInformation(
    "Request completed: {Method} {Path} - Status: {StatusCode} - Duration: {Duration}ms",
    method, path, statusCode, duration);

// ❌ BAD - Unstructured
_logger.LogInformation($"Request completed: {method} {path} with status {statusCode}");
```

### 3. **Correlation ID Propagation**

```csharp
// Middleware già implementato gestisce automaticamente
// Ogni richiesta avrà CorrelationId nei log
```

### 4. **Performance Monitoring**

```csharp
var stopwatch = Stopwatch.StartNew();
// ... operation ...
stopwatch.Stop();

_logger.LogInformation(
    "Operation {Operation} completed in {Duration}ms",
    operationName,
    stopwatch.ElapsedMilliseconds);
```

---

## 🚨 Alerting Setup

### Seq Alerts

1. Vai a **Settings → Alerts**
2. Crea alert:

```sql
-- High error rate
SELECT Count(*) as ErrorCount
FROM stream
WHERE Level = 'Error'
  AND @t >= Now() - 5m
GROUP BY Time(5m)
HAVING ErrorCount > 10
```

### Kibana Alerts (Watcher)

```json
{
  "trigger": {
    "schedule": {
      "interval": "5m"
    }
  },
  "input": {
    "search": {
      "request": {
        "indices": ["microservices-*"],
        "body": {
          "query": {
            "bool": {
              "must": [
                { "term": { "Level": "Error" }},
                { "range": { "@t": { "gte": "now-5m" }}}
              ]
            }
          }
        }
      }
    }
  },
  "condition": {
    "compare": {
      "ctx.payload.hits.total": {
        "gt": 10
      }
    }
  },
  "actions": {
    "send_email": {
      "email": {
        "to": "team@example.com",
        "subject": "High error rate detected",
        "body": "Detected {{ctx.payload.hits.total}} errors in the last 5 minutes"
      }
    }
  }
}
```

---

## 🧪 Testing Logging

### Test Correlation ID Propagation

```bash
# 1. Request con custom correlation ID
curl -H "X-Correlation-ID: test-123" \
     http://localhost:5000/api/auth/login

# 2. Verifica nei log del gateway
grep "test-123" logs/api-gateway-*.json

# 3. Verifica nei log di auth-service
grep "test-123" ../auth-service/AuthService/logs/auth-service-*.json
```

### Test Structured Logging

```bash
# 1. Generate some traffic
./test-gateway.sh

# 2. Query JSON logs
cat logs/api-gateway-*.json | jq -r \
  'select(.Path == "/api/auth/login") | .Duration'

# 3. Calculate average response time
cat logs/api-gateway-*.json | jq -r \
  'select(.Duration != null) | .Duration' | \
  awk '{sum+=$1; count++} END {print sum/count}'
```

---

## 📈 Retention Policies

### File Logs
```csharp
// Configurato in Program.cs
retainedFileCountLimit: 30,  // 30 giorni
fileSizeLimitBytes: 10485760, // 10MB per file
```

### Seq Retention
```bash
# UI: Settings → Retention
# Default: 7 giorni per ambiente Development
# Production: 30+ giorni
```

### Elasticsearch Retention
```json
PUT _ilm/policy/microservices-policy
{
  "policy": {
    "phases": {
      "hot": {
        "actions": {}
      },
      "delete": {
        "min_age": "30d",
        "actions": {
          "delete": {}
        }
      }
    }
  }
}
```

---

## 🔐 Security Considerations

### 1. **No Sensitive Data in Logs**

```csharp
// ✅ GOOD
_logger.LogInformation("User {UserId} logged in", userId);

// ❌ BAD - Never log passwords/tokens!
_logger.LogInformation("Login: {Email} / {Password}", email, password);
```

### 2. **Sanitize User Input**

```csharp
// Use structured logging - Serilog sanitizes automatically
_logger.LogInformation("Searching for {Query}", userInput);
```

### 3. **Secure Log Access**

- ✅ Use authentication for Seq/Kibana
- ✅ HTTPS for log transmission
- ✅ Role-based access control

---

## 📚 Risorse

- [Serilog Documentation](https://serilog.net/)
- [Seq Documentation](https://docs.datalust.co/docs)
- [ELK Stack Guide](https://www.elastic.co/guide/index.html)
- [Application Insights](https://learn.microsoft.com/en-us/azure/azure-monitor/app/app-insights-overview)
- [Structured Logging Best Practices](https://messagetemplates.org/)

---

**🎯 Logging = Osservabilità = Successo in Produzione!**
