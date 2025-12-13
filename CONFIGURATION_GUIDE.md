# 🔧 Guida Configurazione API Gateway

## Configurazione Keycloak

### 1. Realm Configuration

Il gateway si connette al realm Keycloak per validare i JWT token:

```bash
# URL Authority
http://localhost:8080/realms/microservices
```

### 2. Client Configuration

Non è necessario creare un client dedicato per il gateway. Il gateway valida semplicemente i token emessi dal client `frontend-client` già configurato nell'auth-service.

---

## Configurazione CORS per Angular Frontend

### Development

In `appsettings.Development.json`:

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

### Production

In `appsettings.json`:

```json
{
  "Cors": {
    "AllowedOrigins": [
      "https://app.yourdomain.com",
      "https://admin.yourdomain.com"
    ]
  }
}
```

---

## Configurazione Routes YARP

### Struttura Base

```json
{
  "ReverseProxy": {
    "Routes": {
      "route-name": {
        "ClusterId": "cluster-name",
        "Match": {
          "Path": "/api/service/{**catch-all}"
        },
        "Transforms": [
          {
            "PathPattern": "/api/v1/service/{**catch-all}"
          }
        ]
      }
    },
    "Clusters": {
      "cluster-name": {
        "Destinations": {
          "destination-name": {
            "Address": "http://service:port"
          }
        }
      }
    }
  }
}
```

### Esempio: Aggiungere Order Service

```json
{
  "ReverseProxy": {
    "Routes": {
      "orders-route": {
        "ClusterId": "orders-cluster",
        "Match": {
          "Path": "/api/orders/{**catch-all}"
        },
        "Transforms": [
          {
            "PathPattern": "/api/v1/orders/{**catch-all}"
          }
        ]
      }
    },
    "Clusters": {
      "orders-cluster": {
        "Destinations": {
          "orders-service": {
            "Address": "http://orders-service:5002"
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

---

## Rate Limiting Configuration

### Policy per Endpoint

```json
{
  "IpRateLimiting": {
    "EnableEndpointRateLimiting": true,
    "StackBlockedRequests": false,
    "HttpStatusCode": 429,
    "GeneralRules": [
      {
        "Endpoint": "*",
        "Period": "1m",
        "Limit": 100
      },
      {
        "Endpoint": "*/api/auth/login",
        "Period": "1m",
        "Limit": 5
      },
      {
        "Endpoint": "*/api/orders/create",
        "Period": "1m",
        "Limit": 10
      }
    ]
  }
}
```

### IP Whitelist

```json
{
  "IpRateLimitPolicies": {
    "IpRules": [
      {
        "Ip": "192.168.1.100",
        "Rules": [
          {
            "Endpoint": "*",
            "Period": "1m",
            "Limit": 1000
          }
        ]
      }
    ]
  }
}
```

---

## Health Checks Configuration

### Active Health Checks

Verifica attiva periodica:

```json
{
  "HealthCheck": {
    "Active": {
      "Enabled": true,
      "Interval": "00:00:30",
      "Timeout": "00:00:10",
      "Policy": "ConsecutiveFailures",
      "Path": "/health"
    }
  }
}
```

### Passive Health Checks

Rilevamento automatico errori:

```json
{
  "HealthCheck": {
    "Passive": {
      "Enabled": true,
      "Policy": "TransportFailureRate",
      "ReactivationPeriod": "00:01:00"
    }
  }
}
```

---

## Load Balancing Configuration

### Round Robin (Default)

```json
{
  "Clusters": {
    "my-cluster": {
      "LoadBalancingPolicy": "RoundRobin",
      "Destinations": {
        "instance-1": { "Address": "http://service-1:5001" },
        "instance-2": { "Address": "http://service-2:5001" }
      }
    }
  }
}
```

### Least Requests

```json
{
  "LoadBalancingPolicy": "LeastRequests"
}
```

### Random

```json
{
  "LoadBalancingPolicy": "Random"
}
```

---

## Logging Configuration

### Serilog Levels

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft.AspNetCore": "Warning",
        "Yarp": "Information",
        "System": "Warning"
      }
    }
  }
}
```

### File Logging

```csharp
.WriteTo.File(
    path: "logs/api-gateway-.log",
    rollingInterval: RollingInterval.Day,
    retainedFileCountLimit: 30,
    fileSizeLimitBytes: 10485760, // 10MB
    rollOnFileSizeLimit: true
)
```

---

## Environment Variables

### Development

```bash
ASPNETCORE_ENVIRONMENT=Development
ASPNETCORE_URLS=http://+:5000
Keycloak__Authority=http://localhost:8080/realms/microservices
Keycloak__RequireHttpsMetadata=false
```

### Production

```bash
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:5000
Keycloak__Authority=https://keycloak.yourdomain.com/realms/microservices
Keycloak__RequireHttpsMetadata=true
```

---

## Docker Network Configuration

### Create Shared Network

```bash
docker network create microservices-network
```

### Connect Services

```yaml
services:
  api-gateway:
    networks:
      - microservices-network
  
  auth-service:
    networks:
      - microservices-network

networks:
  microservices-network:
    external: true
```

---

## Security Headers Configuration

Configurato automaticamente in `Program.cs`:

```csharp
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    await next();
});
```

Per customizzare, edita il middleware in [Program.cs](ApiGateway/Program.cs).
