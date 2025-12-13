# 🚀 API Gateway - YARP Reverse Proxy

[![Version](https://img.shields.io/badge/version-1.0.0-blue.svg)](https://github.com/yourusername/api-gateway/releases/tag/v1.0.0)
[![.NET](https://img.shields.io/badge/.NET-10.0-purple.svg)](https://dotnet.microsoft.com/)
[![YARP](https://img.shields.io/badge/YARP-2.3.0-green.svg)](https://microsoft.github.io/reverse-proxy/)

API Gateway basato su **YARP (Yet Another Reverse Proxy)** di Microsoft per architettura a microservizi con supporto JWT, Keycloak, Correlation ID e Rate Limiting.

## 🎯 Caratteristiche

### Routing & Load Balancing
- ✅ **YARP Reverse Proxy**: routing dinamico ad alte prestazioni
- ✅ **Active Health Checks**: monitoraggio attivo ogni 30s
- ✅ **Passive Health Checks**: rilevamento automatico errori
- ✅ **Load Balancing**: Round Robin tra destinazioni multiple
- ✅ **Path Transformation**: riscrittura automatica percorsi

### Sicurezza & Autenticazione
- 🔒 **JWT Authentication**: validazione token Keycloak
- 🔒 **HttpOnly Cookies**: supporto cookie sicuri
- 🔒 **Rate Limiting**: protezione da brute-force
  - Login: max 5 richieste/minuto
  - Registrazione: max 3 richieste/minuto
  - Generale: max 100 richieste/minuto
- 🔒 **CORS**: policy configurabile per frontend Angular
- 🔒 **Security Headers**: X-Frame-Options, X-Content-Type-Options, etc.

### Osservabilità
- 📊 **Correlation ID**: tracciamento end-to-end richieste
- 📊 **Serilog Logging**: logging strutturato JSON
- 📊 **Health Checks**: endpoint `/health` aggregato
- 📊 **Request Logging**: log dettagliato di tutte le richieste

### DevOps
- 🐳 **Docker**: containerizzazione completa
- 🐳 **Docker Compose**: orchestrazione multi-service
- ⚡ **Production Ready**: ottimizzato per deployment

---

## 📦 Architettura

```
┌──────────────┐     ┌──────────────────┐     ┌──────────────┐
│   Angular    │────▶│   API Gateway    │────▶│ Auth Service │
│  Frontend    │◀────│   (YARP 2.3)     │◀────│  (.NET 10)   │
└──────────────┘     └──────────────────┘     └──────────────┘
                              │
                              ├────▶ Future Microservice 1
                              ├────▶ Future Microservice 2
                              └────▶ Future Microservice N

                     ┌──────────────────┐
                     │    Keycloak      │
                     │  (JWT Provider)  │
                     └──────────────────┘
```

---

## 🚀 Quick Start

### Prerequisiti
- .NET 10 SDK
- Docker & Docker Compose
- Auth Service funzionante (porta 5001)
- Keycloak configurato

### 1️⃣ Clona e Configura

```bash
cd api-gateway

# Copia file di configurazione
cp .env.example .env

# Modifica .env con le tue impostazioni
nano .env
```

### 2️⃣ Avvio Locale (Development)

```bash
# Restore dependencies
dotnet restore

# Run in development mode
cd ApiGateway
dotnet run
```

API Gateway sarà disponibile su: `http://localhost:5000`

### 3️⃣ Avvio con Docker

```bash
# Build e avvio
docker-compose up -d

# Verifica logs
docker-compose logs -f api-gateway

# Verifica health
curl http://localhost:5000/health
```

---

## 🔧 Configurazione

### Routing Configuration

Il routing è configurato in [appsettings.json](ApiGateway/appsettings.json):

```json
{
  "ReverseProxy": {
    "Routes": {
      "auth-route": {
        "ClusterId": "auth-cluster",
        "Match": {
          "Path": "/api/auth/{**catch-all}"
        },
        "Transforms": [
          {
            "PathPattern": "/api/v1/auth/{**catch-all}"
          }
        ]
      }
    },
    "Clusters": {
      "auth-cluster": {
        "Destinations": {
          "auth-service": {
            "Address": "http://localhost:5001"
          }
        },
        "HealthCheck": {
          "Active": {
            "Enabled": true,
            "Interval": "00:00:30",
            "Path": "/health"
          }
        }
      }
    }
  }
}
```

### Aggiungere un Nuovo Microservizio

Per aggiungere un nuovo microservizio, edita `appsettings.json`:

```json
{
  "ReverseProxy": {
    "Routes": {
      "orders-route": {
        "ClusterId": "orders-cluster",
        "Match": {
          "Path": "/api/orders/{**catch-all}"
        }
      }
    },
    "Clusters": {
      "orders-cluster": {
        "Destinations": {
          "orders-service": {
            "Address": "http://localhost:5002"
          }
        }
      }
    }
  }
}
```

### Keycloak Configuration

```json
{
  "Keycloak": {
    "Authority": "http://localhost:8080/realms/microservices",
    "RequireHttpsMetadata": false,
    "Audience": "account"
  }
}
```

### CORS Configuration

```json
{
  "Cors": {
    "AllowedOrigins": [
      "http://localhost:4200",
      "http://localhost:4201"
    ]
  }
}
```

---

## 📡 Endpoints

### Gateway Management

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/health` | GET | Health check aggregato |
| `/api/gateway/info` | GET | Informazioni gateway |
| `/api/gateway/routes` | GET | Lista route configurate |

### Proxied Routes

| Gateway Route | Target Service | Description |
|---------------|----------------|-------------|
| `/api/auth/**` | Auth Service | Autenticazione e registrazione |

---

## 🔐 Sicurezza

### JWT Authentication

Il gateway valida automaticamente i JWT token:

```bash
# Request con Authorization header
curl -H "Authorization: Bearer YOUR_JWT_TOKEN" \
     http://localhost:5000/api/auth/protected

# Request con HttpOnly cookie
curl -b "access_token=YOUR_JWT_TOKEN" \
     http://localhost:5000/api/auth/protected
```

### Rate Limiting

Configurato in [appsettings.json](ApiGateway/appsettings.json):

```json
{
  "IpRateLimiting": {
    "GeneralRules": [
      {
        "Endpoint": "*/api/auth/login",
        "Period": "1m",
        "Limit": 5
      }
    ]
  }
}
```

### Correlation ID

Ogni richiesta riceve un `X-Correlation-ID`:

```bash
curl -v http://localhost:5000/api/gateway/info

# Response headers:
< X-Correlation-ID: 123e4567-e89b-12d3-a456-426614174000
```

---

## 📊 Monitoring & Logging

### Health Checks

```bash
# Gateway health
curl http://localhost:5000/health

# Response:
{
  "status": "Healthy",
  "totalDuration": "00:00:00.0234567",
  "entries": {
    "gateway_health": {
      "status": "Healthy",
      "description": "Gateway is healthy. Memory: 125MB"
    },
    "auth-service": {
      "status": "Healthy"
    }
  }
}
```

### Logs Structure

I log sono salvati in `logs/api-gateway-YYYYMMDD.log`:

```
[12:34:56 INF] [abc-123-def] Incoming request: GET /api/auth/login from 192.168.1.10
[12:34:56 INF] [abc-123-def] Completed request: GET /api/auth/login - Status: 200 - Duration: 145ms
```

---

## 🐳 Docker Deployment

### Standalone

```bash
# Build
docker build -t api-gateway:latest -f ApiGateway/Dockerfile .

# Run
docker run -d \
  -p 5000:5000 \
  -e Keycloak__Authority=http://keycloak:8080/realms/microservices \
  --name api-gateway \
  api-gateway:latest
```

### Con Docker Compose

```bash
# Start all services
docker-compose up -d

# Stop
docker-compose down

# View logs
docker-compose logs -f api-gateway
```

---

## 🔄 Integrazione con Auth Service

### Network Configuration

Assicurati che auth-service e api-gateway siano sulla stessa rete Docker:

```bash
# Create shared network
docker network create microservices-network

# Verify
docker network inspect microservices-network
```

### Service Discovery

In Docker Compose, i servizi si scoprono automaticamente per nome:

```yaml
environment:
  - ReverseProxy__Clusters__auth-cluster__Destinations__auth-service__Address=http://auth-service:5001
```

---

## 🧪 Testing

### Test Routing

```bash
# Test gateway info
curl http://localhost:5000/api/gateway/info

# Test auth service through gateway
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"user@example.com","password":"password123"}'
```

### Test Health Checks

```bash
# Gateway health
curl http://localhost:5000/health

# Auth service health (through gateway)
curl http://localhost:5000/api/auth/health
```

### Test Correlation ID

```bash
# With custom correlation ID
curl -H "X-Correlation-ID: my-custom-id" \
     http://localhost:5000/api/gateway/info

# Auto-generated correlation ID
curl -v http://localhost:5000/api/gateway/info
```

---

## 📈 Performance

YARP offre prestazioni eccellenti:

- **Latency overhead**: < 1ms in media
- **Throughput**: > 50,000 req/sec (su hardware moderno)
- **Memory footprint**: ~120MB a riposo
- **CPU usage**: minimo, ottimizzato per async I/O

---

## 🛠️ Troubleshooting

### Gateway non raggiunge Auth Service

```bash
# Verifica connettività
docker exec api-gateway curl http://auth-service:5001/health

# Verifica DNS
docker exec api-gateway nslookup auth-service

# Verifica network
docker network inspect microservices-network
```

### JWT Token non validato

```bash
# Verifica Keycloak authority
curl http://localhost:8080/realms/microservices/.well-known/openid-configuration

# Check logs
docker-compose logs api-gateway | grep "JWT"
```

### Rate Limiting troppo restrittivo

Edita `appsettings.json`:

```json
{
  "IpRateLimiting": {
    "GeneralRules": [
      {
        "Endpoint": "*",
        "Period": "1m",
        "Limit": 200  // Aumenta il limite
      }
    ]
  }
}
```

---

## 🔮 Roadmap

- [ ] **Prometheus Metrics**: esposizione metriche per Prometheus
- [ ] **OpenTelemetry**: distributed tracing completo
- [ ] **Redis Cache**: caching distribuito per response
- [ ] **GraphQL Gateway**: supporto GraphQL federation
- [ ] **WebSocket Proxy**: proxy per connessioni WebSocket
- [ ] **Circuit Breaker**: pattern circuit breaker avanzato

---

## 📚 Risorse

- [YARP Documentation](https://microsoft.github.io/reverse-proxy/)
- [.NET 10 Documentation](https://learn.microsoft.com/en-us/dotnet/)
- [Keycloak Documentation](https://www.keycloak.org/documentation)
- [Microservices Patterns](https://microservices.io/patterns/apigateway.html)

---

## 📄 License

MIT License - vedi [LICENSE](LICENSE) per dettagli

---

## 🤝 Contributing

Contributi benvenuti! Per favore:
1. Fork il progetto
2. Crea un feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit le modifiche (`git commit -m 'Add AmazingFeature'`)
4. Push al branch (`git push origin feature/AmazingFeature`)
5. Apri una Pull Request

---

**Made with ❤️ using YARP & .NET 10**
