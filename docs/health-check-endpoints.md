# Health Check Endpoints

This document describes the health check endpoints available in the Mystira API and Admin API services.

## Overview

The Mystira APIs provide three health check endpoints with different purposes:

1. **`/health`** - Comprehensive health check (all dependencies)
2. **`/health/ready`** - Readiness probe (critical dependencies only)
3. **`/health/live`** - Liveness probe (simple app running check)

## Endpoints

### `/health` - Comprehensive Health Check

**Purpose**: Full health check that verifies all system dependencies.

**Use Cases**:
- Monitoring dashboards
- Comprehensive system health verification
- Debugging production issues

**Checks Performed**:
- Cosmos DB connectivity
- Blob Storage connectivity
- Discord Bot status (if enabled)

**Response Format**:
```json
{
  "status": "Healthy",
  "duration": "00:00:00.1234567",
  "results": {
    "blob_storage": {
      "Status": "Healthy",
      "Description": "Blob storage connection is healthy",
      "Duration": "00:00:00.0456789",
      "Data": {},
      "Exception": null
    },
    "cosmos_db": {
      "Status": "Healthy",
      "Description": "Cosmos DB connection is healthy",
      "Duration": "00:00:00.0234567",
      "Data": {},
      "Exception": null
    }
  }
}
```

**HTTP Status Codes**:
- `200 OK` - System is healthy or degraded
- `503 Service Unavailable` - System is unhealthy

---

### `/health/ready` - Readiness Probe ✅ **RECOMMENDED FOR DEPLOYMENTS**

**Purpose**: Verifies that the application is ready to receive traffic. Checks only critical dependencies required for the app to function.

**Use Cases**:
- **Deployment health checks** (CI/CD pipelines)
- Kubernetes readiness probes
- Load balancer health checks
- Database initialization verification

**Checks Performed**:
- Cosmos DB connectivity (tagged with "ready")

**Response Format**:
```json
{
  "status": "Healthy",
  "checks": [
    {
      "name": "cosmos_db",
      "status": "Healthy",
      "description": "Cosmos DB connection is healthy",
      "duration": 23.456
    }
  ],
  "totalDuration": 23.456
}
```

**HTTP Status Codes**:
- `200 OK` - Application is ready to receive traffic
- `503 Service Unavailable` - Application is not ready (database not initialized, etc.)

**Example Usage in CI/CD**:
```bash
# Wait for deployment to stabilize
sleep 60

# Check readiness endpoint
HEALTH_URL="https://your-api.azurewebsites.net/health/ready"
for i in {1..10}; do
  HTTP_CODE=$(curl -s -o /dev/null -w "%{http_code}" "$HEALTH_URL")
  if [ "$HTTP_CODE" = "200" ]; then
    echo "✅ Application is ready"
    exit 0
  fi
  sleep 15
done
```

---

### `/health/live` - Liveness Probe

**Purpose**: Simple check to verify the application is running (not hung or deadlocked).

**Use Cases**:
- Kubernetes liveness probes
- Simple uptime monitoring
- Container orchestration health checks

**Checks Performed**:
- None - returns healthy if the app can respond to requests

**Response**: ASP.NET Core default health check response

**HTTP Status Codes**:
- `200 OK` - Application is running
- No response - Application is hung or crashed

---

## Deployment Health Check Strategy

For deployment pipelines, we recommend the following strategy:

### 1. Initial Wait Period (60 seconds)
Allow the application and database initialization to start:
```bash
echo "Waiting for deployment to stabilize and database initialization to complete..."
sleep 60
```

### 2. Readiness Check Loop (10 attempts × 15 seconds)
Check the `/health/ready` endpoint to verify database initialization:
```bash
HEALTH_URL="https://your-api.azurewebsites.net/health/ready"

for i in {1..10}; do
  echo "Attempt $i/10..."
  HTTP_CODE=$(curl -s -o /dev/null -w "%{http_code}" "$HEALTH_URL")
  
  if [ "$HTTP_CODE" = "200" ]; then
    RESPONSE=$(curl -s "$HEALTH_URL")
    if echo "$RESPONSE" | grep -q '"status":"Healthy"'; then
      echo "✅ Database initialization confirmed"
      exit 0
    fi
  fi
  
  if [ $i -lt 10 ]; then
    sleep 15
  fi
done

echo "❌ Health check failed after 10 attempts (~4 minutes)"
exit 1
```

### 3. Total Timeout
- Initial wait: 60 seconds
- Retry loop: 10 attempts × 15 seconds = 150 seconds
- **Total: ~4 minutes**

This timeout accounts for:
- Azure App Service cold start
- Application startup
- Database initialization
- Master data seeding (if enabled)

---

## Configuration

### Enabling/Disabling Health Checks

Health checks are automatically configured based on environment:

**Cosmos DB Health Check**:
- Enabled when Cosmos DB connection string is configured
- Tagged with: `"ready"`, `"db"`

**Blob Storage Health Check**:
- Always enabled

**Discord Bot Health Check**:
- Enabled when `Discord:Enabled = true`

### Database Initialization

Database initialization is controlled by configuration settings:

```json
{
  "InitializeDatabaseOnStartup": true,
  "SeedMasterDataOnStartup": true
}
```

These settings affect the `/health/ready` endpoint behavior:
- Database initialization happens **before** the app starts accepting requests
- The `/health/ready` endpoint will return `503` until database is initialized
- Deployment health checks will wait for initialization to complete

---

## Troubleshooting

### Health Check Returns 503 During Deployment

**Symptom**: `/health/ready` returns HTTP 503

**Possible Causes**:
1. Database initialization still in progress
2. Cosmos DB connection issues
3. Missing database containers/permissions

**Solution**:
1. Check application logs for initialization errors
2. Verify Cosmos DB connection string is correct
3. Verify app identity has permissions to create/read containers
4. Increase health check timeout in deployment pipeline
5. Check `InitializeDatabaseOnStartup` configuration setting

### Health Check Timeouts

**Symptom**: Deployment health check fails after maximum retries

**Possible Causes**:
1. Database initialization taking longer than expected
2. Network connectivity issues
3. Cosmos DB throttling

**Solution**:
1. Check Azure Portal application logs
2. Verify Cosmos DB provisioned throughput (RU/s)
3. Check for Cosmos DB service health issues
4. Consider increasing deployment timeout

---

## Best Practices

1. **Use `/health/ready` for deployment checks** - It verifies critical dependencies
2. **Use `/health/live` for liveness probes** - It's lightweight and fast
3. **Use `/health` for monitoring** - It provides comprehensive dependency status
4. **Set appropriate timeouts** - Database initialization can take 2-4 minutes on first deployment
5. **Validate response body** - Don't just check HTTP status code, verify the "status" field
6. **Monitor health check duration** - Slow health checks may indicate performance issues

---

## Related Documentation

- [Azure Health Check Configuration](./monitoring-setup.md)
- [Database Initialization](./STARTUP_CONFIGURATION.md)
- [Troubleshooting Guide](./troubleshooting.md)
- [Deployment Strategy](./devops/deployment-strategy.md)
