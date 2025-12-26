# Certificate Renewal

**Severity**: Medium  
**Time to Complete**: 30-60 minutes  
**Prerequisites**: SSL/TLS certificate expiring or expired

---

## Overview

This runbook guides you through renewing SSL/TLS certificates for Mystira application domains.

## Certificate Locations

| Domain | Certificate Type | Managed By |
|--------|-----------------|------------|
| mystira.app | Azure Front Door | Azure (auto-renew) |
| api.mystira.app | App Service | Azure (auto-renew) |
| *.mystira.app | Wildcard | Let's Encrypt/Manual |
| Custom domains | Varies | Manual |

## Azure-Managed Certificates

### Verification

Azure-managed certificates auto-renew. Verify they're working:

```bash
# Check App Service certificate
az webapp config ssl list \
  --resource-group rg-mystira-prod

# Check expiration dates
az webapp config ssl show \
  --resource-group rg-mystira-prod \
  --certificate-name [cert-name]
```

### If Auto-Renewal Fails

1. Check domain validation requirements
2. Verify DNS records are correct
3. Ensure App Service has required permissions
4. Contact Azure Support if issue persists

## Manual Certificate Renewal (Let's Encrypt)

### Step 1: Generate New Certificate

Using Certbot:

```bash
# Install certbot
sudo apt-get install certbot

# Generate certificate
sudo certbot certonly --manual \
  --preferred-challenges dns \
  -d mystira.app \
  -d *.mystira.app

# Follow prompts to add DNS TXT records
```

### Step 2: Add DNS Validation Records

Add the TXT record to your DNS:

```bash
# Example TXT record
_acme-challenge.mystira.app TXT "[validation-string]"
```

Wait for DNS propagation (usually 5-15 minutes):

```bash
# Verify DNS propagation
dig _acme-challenge.mystira.app TXT +short
```

### Step 3: Complete Validation

Continue certbot process after DNS verification.

### Step 4: Upload to Azure

```bash
# Export certificate
sudo openssl pkcs12 -export \
  -in /etc/letsencrypt/live/mystira.app/fullchain.pem \
  -inkey /etc/letsencrypt/live/mystira.app/privkey.pem \
  -out mystira-app.pfx

# Upload to Azure Key Vault
az keyvault certificate import \
  --vault-name kv-mystira-prod \
  --name mystira-app-ssl \
  --file mystira-app.pfx

# Bind to App Service
az webapp config ssl upload \
  --resource-group rg-mystira-prod \
  --name mys-prod-mystira-api-san \
  --certificate-file mystira-app.pfx

az webapp config ssl bind \
  --resource-group rg-mystira-prod \
  --name mys-prod-mystira-api-san \
  --certificate-thumbprint [thumbprint] \
  --ssl-type SNI
```

## Certificate Monitoring

### Set Up Expiration Alerts

```bash
# Create alert for certificate expiration
az monitor metrics alert create \
  --name cert-expiration-alert \
  --resource-group rg-mystira-prod \
  --scopes [certificate-resource-id] \
  --condition "avg CertificateExpiration < 30" \
  --description "SSL certificate expiring in 30 days"
```

### Check Certificate Validity

```bash
# Check certificate details
openssl s_client -connect api.mystira.app:443 -servername api.mystira.app < /dev/null | openssl x509 -noout -dates

# Check expiration
echo | openssl s_client -servername api.mystira.app -connect api.mystira.app:443 2>/dev/null | openssl x509 -noout -enddate
```

## Verification

### Success Criteria

- [ ] Certificate valid and trusted
- [ ] No browser warnings
- [ ] HTTPS connections working
- [ ] Certificate expiration > 30 days
- [ ] All domains covered

### Test HTTPS Endpoints

```bash
# Test API endpoint
curl -v https://api.mystira.app/health 2>&1 | grep "SSL certificate verify ok"

# Test main domain
curl -v https://mystira.app 2>&1 | grep "SSL certificate verify ok"

# Use SSL Labs
# https://www.ssllabs.com/ssltest/analyze.html?d=api.mystira.app
```

## Troubleshooting

### Issue: Certificate validation failing

**Resolution**:
1. Verify DNS records are correct
2. Check domain ownership
3. Ensure no firewall blocking validation
4. Clear CDN cache if using one

### Issue: Old certificate still being served

**Resolution**:
1. Clear browser cache
2. Restart App Service
3. Clear CDN/Front Door cache
4. Verify binding in Azure Portal

### Issue: Mixed content warnings

**Resolution**:
1. Ensure all resources loaded over HTTPS
2. Update absolute URLs to use HTTPS
3. Implement HSTS headers
4. Check for hardcoded HTTP URLs

## Automation

### Set Up Auto-Renewal

Create Azure Function or Logic App:

```bash
# Example: Schedule certbot renewal
# Add to cron: 0 0 1 * * certbot renew --quiet
```

### Monitor Certificate Expiration

```kql
// Alert query
AzureDiagnostics
| where ResourceProvider == "MICROSOFT.WEB"
| where Category == "AppServiceCertificateLogs"
| where TimeGenerated > ago(24h)
| where DaysToExpiration < 30
| project TimeGenerated, CertificateName, DaysToExpiration
```

## Post-Procedure

- [ ] Update certificate inventory
- [ ] Document renewal process
- [ ] Set calendar reminders (90, 60, 30 days before expiration)
- [ ] Test all HTTPS endpoints
- [ ] Update monitoring dashboard

## Related Documentation

- [DNS Configuration](../dns-configuration.md)
- [Security Best Practices](../rbac-and-managed-identity.md)
- [Monitoring Setup](../slo-definitions.md)
