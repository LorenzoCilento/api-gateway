# 🔧 Environment Variables Setup Guide

## Quick Start

```bash
# 1. Copy example file to .env
cp .env.example .env

# 2. Edit .env with your values
nano .env

# 3. Start services
docker-compose up -d
```

---

## 📋 Configuration Categories

### 1. **Application Settings** (Required)

```bash
ASPNETCORE_ENVIRONMENT=Development
ASPNETCORE_URLS=http://+:5000
```

**Environments:**
- `Development`: Verbose logs, Swagger enabled, relaxed security
- `Staging`: Production-like with more logging
- `Production`: Optimized, minimal logs, strict security

### 2. **Keycloak Settings** (Required)

```bash
KEYCLOAK_AUTHORITY=http://keycloak:8080/realms/microservices
KEYCLOAK_REQUIRE_HTTPS=false
```

**Development:**
```bash
KEYCLOAK_AUTHORITY=http://localhost:8080/realms/microservices
KEYCLOAK_REQUIRE_HTTPS=false
```

**Production:**
```bash
KEYCLOAK_AUTHORITY=https://keycloak.yourdomain.com/realms/microservices
KEYCLOAK_REQUIRE_HTTPS=true
```

### 3. **Microservices URLs** (Required)

```bash
AUTH_SERVICE_URL=http://auth-service:5001
```

**Local Development:**
```bash
AUTH_SERVICE_URL=http://localhost:5001
```

**Docker Compose:**
```bash
AUTH_SERVICE_URL=http://auth-service:5001
```

**Kubernetes:**
```bash
AUTH_SERVICE_URL=http://auth-service.default.svc.cluster.local:5001
```

**Remote Service:**
```bash
AUTH_SERVICE_URL=https://auth.yourcompany.com
```

### 4. **CORS Settings** (Required)

```bash
ALLOWED_ORIGIN_1=http://localhost:4200
ALLOWED_ORIGIN_2=http://localhost:4201
```

**Multiple Origins:**
- Add as many as needed: `ALLOWED_ORIGIN_3`, `ALLOWED_ORIGIN_4`, etc.
- Must match exactly (including protocol and port)

**Examples:**
```bash
# Local Angular dev
ALLOWED_ORIGIN_1=http://localhost:4200

# Local React dev
ALLOWED_ORIGIN_2=http://localhost:3000

# Production
ALLOWED_ORIGIN_3=https://app.yourdomain.com
ALLOWED_ORIGIN_4=https://admin.yourdomain.com
```

### 5. **Rate Limiting** (Optional)

```bash
RATE_LIMIT_GENERAL=100
RATE_LIMIT_GENERAL_PERIOD=1m
RATE_LIMIT_LOGIN=5
RATE_LIMIT_LOGIN_PERIOD=1m
RATE_LIMIT_REGISTER=3
RATE_LIMIT_REGISTER_PERIOD=1m
```

**Periods:**
- `1s` = 1 second
- `1m` = 1 minute
- `1h` = 1 hour
- `1d` = 1 day

**Disable Rate Limiting:**
```bash
RATE_LIMIT_GENERAL=999999
```

### 6. **Centralized Logging** (Optional)

```bash
# Seq
SEQ_URL=http://seq:5341
SEQ_API_KEY=

# Leave empty to disable
SEQ_URL=
```

**With Infrastructure:**
```bash
SEQ_URL=http://seq:5341
```

**Without Infrastructure:**
```bash
SEQ_URL=
# Logs will go to console + file only
```

---

## 🌍 Environment-Specific Configurations

### Development

```bash
# .env.development
ASPNETCORE_ENVIRONMENT=Development
ASPNETCORE_URLS=http://+:5000
KEYCLOAK_AUTHORITY=http://localhost:8080/realms/microservices
KEYCLOAK_REQUIRE_HTTPS=false
AUTH_SERVICE_URL=http://localhost:5001
ALLOWED_ORIGIN_1=http://localhost:4200
RATE_LIMIT_GENERAL=1000
SEQ_URL=http://localhost:5341
```

### Staging

```bash
# .env.staging
ASPNETCORE_ENVIRONMENT=Staging
ASPNETCORE_URLS=http://+:5000
KEYCLOAK_AUTHORITY=https://keycloak-staging.yourdomain.com/realms/microservices
KEYCLOAK_REQUIRE_HTTPS=true
AUTH_SERVICE_URL=http://auth-service:5001
ALLOWED_ORIGIN_1=https://staging-app.yourdomain.com
RATE_LIMIT_GENERAL=500
SEQ_URL=http://seq:5341
```

### Production

```bash
# .env.production
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:5000
KEYCLOAK_AUTHORITY=https://keycloak.yourdomain.com/realms/microservices
KEYCLOAK_REQUIRE_HTTPS=true
AUTH_SERVICE_URL=http://auth-service:5001
ALLOWED_ORIGIN_1=https://app.yourdomain.com
ALLOWED_ORIGIN_2=https://admin.yourdomain.com
RATE_LIMIT_GENERAL=100
RATE_LIMIT_LOGIN=3
RATE_LIMIT_REGISTER=2
SEQ_URL=
# Use CloudWatch, Application Insights, or ELK in production
```

---

## 🔄 Switching Environments

### Docker Compose

```bash
# Development
docker-compose --env-file .env.development up -d

# Staging
docker-compose --env-file .env.staging up -d

# Production
docker-compose --env-file .env.production up -d
```

### Override with CLI

```bash
# Override specific variables
docker-compose up -d \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e SEQ_URL=http://seq:5341
```

---

## 🛡️ Security Best Practices

### 1. **Never Commit .env**

```bash
# .gitignore already includes
.env
.env.local
.env.*.local
```

### 2. **Use Secrets in Production**

**Docker Swarm:**
```yaml
secrets:
  keycloak_authority:
    external: true

services:
  api-gateway:
    secrets:
      - keycloak_authority
    environment:
      - KEYCLOAK_AUTHORITY_FILE=/run/secrets/keycloak_authority
```

**Kubernetes:**
```yaml
apiVersion: v1
kind: Secret
metadata:
  name: gateway-secrets
type: Opaque
data:
  keycloak-authority: base64encoded
---
apiVersion: apps/v1
kind: Deployment
spec:
  template:
    spec:
      containers:
      - name: api-gateway
        env:
        - name: KEYCLOAK_AUTHORITY
          valueFrom:
            secretKeyRef:
              name: gateway-secrets
              key: keycloak-authority
```

### 3. **Rotate Sensitive Values**

```bash
# Regularly change:
- SEQ_API_KEY
- KEYCLOAK_CLIENT_SECRET (if added)
- Any API keys
```

---

## 🧪 Validation

### Check Variables

```bash
# Show all environment variables in container
docker exec api-gateway env | grep -E "ASPNETCORE|KEYCLOAK|SEQ"

# Test configuration
docker exec api-gateway curl -s http://localhost:5000/api/gateway/info | jq .
```

### Debug Missing Variables

```bash
# Check docker-compose loads .env
docker-compose config

# Verify specific variable
docker-compose config | grep AUTH_SERVICE_URL
```

---

## 📝 Template for New Deployments

```bash
# 1. Copy template
cp .env.example .env

# 2. Fill required values
cat > .env << 'EOF'
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:5000
KEYCLOAK_AUTHORITY=https://your-keycloak.com/realms/your-realm
KEYCLOAK_REQUIRE_HTTPS=true
AUTH_SERVICE_URL=http://your-auth-service:5001
ALLOWED_ORIGIN_1=https://your-frontend.com
RATE_LIMIT_GENERAL=100
SEQ_URL=
EOF

# 3. Deploy
docker-compose up -d
```

---

## ❓ Troubleshooting

### Variables Not Applied

```bash
# 1. Recreate containers
docker-compose down
docker-compose up -d

# 2. Force rebuild
docker-compose up -d --build --force-recreate
```

### CORS Issues

```bash
# Check allowed origins
docker exec api-gateway env | grep ALLOWED_ORIGIN

# Verify they match your frontend URL exactly
curl -H "Origin: http://localhost:4200" \
     -H "Access-Control-Request-Method: POST" \
     -X OPTIONS \
     http://localhost:5000/api/auth/login -v
```

### Rate Limit Too Restrictive

```bash
# Temporarily increase limits
echo "RATE_LIMIT_GENERAL=1000" >> .env
docker-compose restart api-gateway
```

---

## 📚 References

- [ASP.NET Core Configuration](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/)
- [Docker Compose Environment Variables](https://docs.docker.com/compose/environment-variables/)
- [12-Factor App Config](https://12factor.net/config)
