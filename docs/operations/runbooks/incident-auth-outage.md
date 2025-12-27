# Incident Response: Authentication Outage

**Severity**: Critical  
**Time to Complete**: 15-30 minutes  
**Prerequisites**: Authentication system failure detected

---

## Overview

This runbook guides you through responding to authentication system outages that prevent users from signing in or accessing the application.

## Alert Triggers

- Auth endpoint error rate > 10%
- JWT token validation failures spiking
- Unable to reach authentication provider
- Password reset emails not sending

## Initial Assessment (2 minutes)

### Step 1: Verify Auth Status

```bash
# Test authentication endpoints
curl https://api.mystira.app/api/auth/signin -X POST \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com","method":"passwordless"}'

# Check token validation
curl https://api.mystira.app/health \
  -H "Authorization: Bearer $TEST_TOKEN"
```

### Step 2: Check Application Insights

```kql
// Auth endpoint failures
requests
| where timestamp > ago(15m)
| where url contains "/api/auth"
| summarize FailureRate = 100.0 * countif(success == false) / count() by name
```

### Step 3: Determine Impact

- [ ] Users cannot sign in at all (Critical)
- [ ] Existing sessions working but refreshes failing (High)
- [ ] Specific auth methods failing (Medium)
- [ ] Delayed auth emails (Medium)

## Common Scenarios

### Scenario A: JWT Signing Key Issues

**Symptoms**: "Invalid signature" errors on token validation

**Resolution**:

```bash
# 1. Check Key Vault for JWT keys
az keyvault secret show \
  --vault-name kv-mystira-prod \
  --name JwtSettings--RsaPublicKey

# 2. Verify JWKS endpoint (if using)
curl https://auth.mystira.app/.well-known/jwks.json

# 3. Check for key rotation issues
# Review recent secret updates in Key Vault

# 4. Restart app service to reload keys
az webapp restart \
  --name mys-prod-mystira-api-san \
  --resource-group rg-mystira-prod
```

### Scenario B: Email Service Failure

**Symptoms**: Passwordless sign-in codes not being delivered

**Resolution**:

```bash
# 1. Check SendGrid/ACS status
# View Application Insights dependencies

# 2. Verify email service connection string
az keyvault secret show \
  --vault-name kv-mystira-prod \
  --name AzureCommunicationServices--ConnectionString

# 3. Check email quotas/limits
# Review SendGrid dashboard

# 4. Test email sending manually
# Use Kudu console or Azure CLI
```

### Scenario C: Rate Limiting Blocking Users

**Symptoms**: 429 errors on auth endpoints, legitimate users blocked

**Resolution**:

```bash
# 1. Review security metrics
# Check if legitimate traffic or attack

# 2. Temporarily increase rate limits
# Update configuration in Azure Portal
# Set AuthRateLimiting:PermitLimit higher

# 3. Clear rate limit cache if needed
# Restart Redis cache or app service

# 4. Whitelist specific IPs if attack identified
```

### Scenario D: Database Connection Issues

**Symptoms**: Cannot retrieve user accounts, "Database unavailable"

**Resolution**:

See [Database Issues Runbook](./incident-database.md)

```bash
# Quick check
curl https://api.mystira.app/health/ready
```

### Scenario E: Upstream Auth Provider Down

**Symptoms**: Cannot connect to Entra External ID, etc.

**Resolution**:

```bash
# 1. Check provider status page
# Entra External ID: status.azure.com
# Azure AD: status.azure.com

# 2. Verify network connectivity
# Check firewall rules

# 3. Implement fallback if available
# Switch to alternative auth method
# Enable emergency access mode
```

## Emergency Actions

### Enable Emergency Access Mode

If auth system completely unavailable:

```bash
# Enable bypass mode for critical operations
# Update app configuration
az webapp config appsettings set \
  --resource-group rg-mystira-prod \
  --name mys-prod-mystira-api-san \
  --settings AUTH_EMERGENCY_MODE=true

# WARNING: Only enable temporarily
# Disables normal auth checks - use with extreme caution
```

### Communication to Users

Post status update:
```
🚨 We're experiencing authentication issues. We're working to resolve this quickly.
- Unable to sign in: [Y/N]
- Existing sessions affected: [Y/N]
- ETA for resolution: [time]
```

## Verification

### Success Criteria

- [ ] Users can sign in successfully
- [ ] Token validation working
- [ ] Auth endpoint error rate < 1%
- [ ] Email delivery working (if applicable)
- [ ] No rate limiting false positives

### Test Auth Flow

```bash
# 1. Request passwordless code
curl https://api.mystira.app/api/auth/passwordless/signin \
  -X POST -H "Content-Type: application/json" \
  -d '{"email":"test@example.com"}'

# 2. Verify code (use code from email)
curl https://api.mystira.app/api/auth/passwordless/signin/verify \
  -X POST -H "Content-Type: application/json" \
  -d '{"email":"test@example.com","code":"123456"}'

# 3. Test token refresh
curl https://api.mystira.app/api/auth/refresh \
  -X POST -H "Content-Type: application/json" \
  -d '{"refreshToken":"[token]"}'
```

## Post-Incident

### Step 1: Disable Emergency Mode

```bash
# Remove emergency bypass if enabled
az webapp config appsettings delete \
  --resource-group rg-mystira-prod \
  --name mys-prod-mystira-api-san \
  --setting-names AUTH_EMERGENCY_MODE
```

### Step 2: Review Security Logs

Check for unauthorized access during incident:

```kql
customEvents
| where timestamp > ago(1h)
| where name == "Auth.UnauthorizedAccess"
| project timestamp, user_Id, client_IP, customDimensions
```

### Step 3: Update Documentation

- [ ] Document root cause
- [ ] Update runbook if needed
- [ ] Add preventive measures
- [ ] Update monitoring alerts

### Step 4: Implement Preventions

- [ ] Add health checks for auth dependencies
- [ ] Implement circuit breakers
- [ ] Add fallback mechanisms
- [ ] Improve error messages

## Troubleshooting

### Issue: Auth working but slow

**Resolution**:
1. Check token validation performance
2. Review JWKS caching
3. Check database query performance for user lookups

### Issue: Specific users cannot auth

**Resolution**:
1. Check account status in database
2. Verify email address validity
3. Check for account lockouts
4. Review user-specific errors in logs

## Related Documentation

- [High Error Rate Runbook](./incident-high-error-rate.md)
- [Database Issues Runbook](./incident-database.md)
- [Security Documentation](../rbac-and-managed-identity.md)

## Post-Procedure

- [ ] Complete incident report
- [ ] Review auth flow resilience
- [ ] Update monitoring alerts
- [ ] Test failover procedures
- [ ] Schedule postmortem within 24 hours
