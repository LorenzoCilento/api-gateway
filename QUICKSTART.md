# 🚀 Quick Start Guide

Guida rapida per avviare l'API Gateway in 5 minuti.

---

## 📋 Prerequisiti

- ✅ .NET 10 SDK installato
- ✅ Docker & Docker Compose (opzionale, per deployment containerizzato)
- ✅ Auth Service avviato su porta 5001
- ✅ Keycloak avviato su porta 8080

---

## 🏃 Avvio Rapido

### Opzione 1: Development (senza Docker)

```bash
# 1. Clona/naviga nella directory
cd api-gateway

# 2. Restore packages
dotnet restore

# 3. Avvia il gateway
cd ApiGateway
dotnet run

# ✅ Gateway attivo su http://localhost:5000
```

### Opzione 2: Docker (Production-like)

```bash
# 1. Crea network condiviso
docker network create microservices-network

# 2. Avvia con docker-compose
docker-compose up -d

# 3. Verifica status
docker-compose ps

# 4. Verifica logs
docker-compose logs -f api-gateway

# ✅ Gateway attivo su http://localhost:5000
```

---

## ✅ Verifica Funzionamento

### 1. Health Check

```bash
curl http://localhost:5000/health
```

**Risposta attesa:**
```json
{
  "status": "Healthy",
  "entries": {
    "gateway_health": { "status": "Healthy" },
    "auth-service": { "status": "Healthy" }
  }
}
```

### 2. Gateway Info

```bash
curl http://localhost:5000/api/gateway/info
```

**Risposta attesa:**
```json
{
  "service": "API Gateway",
  "version": "1.0.0",
  "framework": "YARP (Yet Another Reverse Proxy)"
}
```

### 3. Test Routing (Login attraverso Gateway)

```bash
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "user@example.com",
    "password": "YourPassword123!"
  }'
```

**Risposta attesa:**
```json
{
  "success": true,
  "message": "Login successful",
  "data": {
    "accessToken": "eyJhbGciOiJSUzI1...",
    "refreshToken": "eyJhbGciOiJIUzI1..."
  }
}
```

---

## 🔧 Configurazione Base

### 1. Configura CORS per il tuo Frontend

Edita `appsettings.json`:

```json
{
  "Cors": {
    "AllowedOrigins": [
      "http://localhost:4200",  // Il tuo Angular frontend
      "http://localhost:4201"   // Eventuale secondo frontend
    ]
  }
}
```

### 2. Verifica URL Auth Service

Edita `appsettings.json`:

```json
{
  "ReverseProxy": {
    "Clusters": {
      "auth-cluster": {
        "Destinations": {
          "auth-service": {
            "Address": "http://localhost:5001"  // Verifica porta corretta
          }
        }
      }
    }
  }
}
```

### 3. Verifica Keycloak Authority

```json
{
  "Keycloak": {
    "Authority": "http://localhost:8080/realms/microservices"
  }
}
```

---

## 🧪 Test Completo

### Script di Test

Crea `test-gateway.sh`:

```bash
#!/bin/bash

echo "🧪 Testing API Gateway..."

# 1. Health Check
echo "1️⃣ Testing /health..."
curl -s http://localhost:5000/health | jq

# 2. Gateway Info
echo "2️⃣ Testing /api/gateway/info..."
curl -s http://localhost:5000/api/gateway/info | jq

# 3. Test CORS
echo "3️⃣ Testing CORS..."
curl -s -H "Origin: http://localhost:4200" \
     -H "Access-Control-Request-Method: POST" \
     -X OPTIONS \
     http://localhost:5000/api/auth/login -v

# 4. Test Routing
echo "4️⃣ Testing routing to auth-service..."
curl -s -X POST http://localhost:5000/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com",
    "password": "TestPassword123!",
    "username": "testuser"
  }' | jq

echo "✅ Tests completed!"
```

Esegui:

```bash
chmod +x test-gateway.sh
./test-gateway.sh
```

---

## 📱 Integrazione con Angular Frontend

### 1. Configura Base URL

Nel tuo `environment.ts`:

```typescript
export const environment = {
  production: false,
  apiUrl: 'http://localhost:5000/api'  // API Gateway URL
};
```

### 2. Auth Service Example

```typescript
import { HttpClient } from '@angular/common/http';
import { environment } from '../environments/environment';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private apiUrl = `${environment.apiUrl}/auth`;

  constructor(private http: HttpClient) {}

  login(email: string, password: string) {
    return this.http.post(`${this.apiUrl}/login`, { email, password });
  }

  register(data: any) {
    return this.http.post(`${this.apiUrl}/register`, data);
  }
}
```

### 3. HTTP Interceptor per Correlation ID

```typescript
import { Injectable } from '@angular/core';
import { HttpInterceptor, HttpRequest, HttpHandler } from '@angular/common/http';

@Injectable()
export class CorrelationIdInterceptor implements HttpInterceptor {
  intercept(req: HttpRequest<any>, next: HttpHandler) {
    const correlationId = sessionStorage.getItem('correlationId') || this.generateId();
    
    const clonedRequest = req.clone({
      headers: req.headers.set('X-Correlation-ID', correlationId)
    });

    return next.handle(clonedRequest);
  }

  private generateId(): string {
    const id = crypto.randomUUID();
    sessionStorage.setItem('correlationId', id);
    return id;
  }
}
```

---

## 🐛 Troubleshooting

### Gateway non si avvia

**Problema:** Porta 5000 già in uso

**Soluzione:**
```bash
# Cambia porta in appsettings.json
"ASPNETCORE_URLS": "http://+:5050"

# Oppure via environment variable
export ASPNETCORE_URLS=http://+:5050
dotnet run
```

### Auth Service non raggiungibile

**Problema:** Gateway non riesce a contattare auth-service

**Verifica:**
```bash
# Test connettività
curl http://localhost:5001/health

# Se in Docker, verifica network
docker network inspect microservices-network

# Verifica che auth-service sia nella stessa network
docker inspect auth-service | grep NetworkMode
```

### CORS Error dal Frontend

**Problema:** Browser blocca richieste per CORS

**Soluzione:** Assicurati che l'origine del frontend sia in `AllowedOrigins`:

```json
{
  "Cors": {
    "AllowedOrigins": [
      "http://localhost:4200"
    ]
  }
}
```

Riavvia il gateway dopo la modifica.

### Rate Limit troppo aggressivo

**Problema:** Ricevi errore 429 (Too Many Requests)

**Soluzione temporanea:** Disabilita rate limiting in `appsettings.Development.json`:

```json
{
  "IpRateLimiting": {
    "EnableEndpointRateLimiting": false
  }
}
```

---

## 📊 Monitoring

### Verifica Logs in Real-time

```bash
# Se avviato con dotnet run
tail -f ApiGateway/logs/api-gateway-*.log

# Se avviato con Docker
docker-compose logs -f api-gateway
```

### Formato Log

```
[12:34:56 INF] [correlation-id-123] Incoming request: GET /api/auth/login from 192.168.1.10
[12:34:56 INF] [correlation-id-123] Completed request: GET /api/auth/login - Status: 200 - Duration: 145ms
```

---

## 🎉 Prossimi Passi

✅ Gateway funzionante? Ottimo!

Ora puoi:

1. **Aggiungere nuovi microservizi**: Edita `appsettings.json` per aggiungere nuove route
2. **Configurare Production**: Vedi [CONFIGURATION_GUIDE.md](CONFIGURATION_GUIDE.md)
3. **Deploy su Cloud**: Kubernetes, Azure, AWS, ecc.
4. **Monitoring avanzato**: Prometheus, Grafana, Application Insights

---

**🚀 Happy coding!**
