# Azure Custom Domain Setup Guide

## Overview
This guide explains how to configure custom domains for App Services in Azure, including SSL certificate management.

## Prerequisites
- Azure subscription with appropriate permissions
- DNS zone in Azure or ability to configure DNS
- App Service already deployed

## Issue: "No binding" Status for Custom Domain

### Symptoms
In Azure Portal → App Service → Custom domains, you see:
- Custom domain listed (e.g., `api.dev.mystira.app`)
- Status shows **"No binding"** with a red X icon

### Root Cause
The hostname binding hasn't been created yet, or the SSL certificate hasn't been configured. This happens when:
1. DNS CNAME record doesn't exist or isn't propagating yet
2. `enableCustomDomain` parameter is `false` in Bicep deployment
3. Infrastructure wasn't deployed after DNS was configured

## Solution

### Step 1: Verify DNS Configuration

Check that your CNAME record is configured correctly:

```bash
# Check DNS propagation
nslookup api.dev.mystira.app

# Should return something like:
# Non-authoritative answer:
# api.dev.mystira.app     canonical name = mys-dev-mystira-api-san.azurewebsites.net
```

If DNS isn't configured, create the CNAME record:
- **Name**: `api.dev` (or subdomain)
- **Type**: CNAME
- **Value**: `<your-app-service>.azurewebsites.net`

### Step 2: Enable Custom Domain in Bicep Parameters

Edit `infrastructure/params.dev.json` (or appropriate environment file):

```json
{
  "parameters": {
    "enableApiCustomDomain": {
      "value": true
    },
    "enableManagedCert": {
      "value": false  // Set to true AFTER hostname binding exists
    }
  }
}
```

### Step 3: Deploy Infrastructure to Create Hostname Binding

```bash
# Navigate to infrastructure directory
cd infrastructure

# Deploy with parameters
az deployment group create \
  --resource-group mys-dev-mystira-rg-san \
  --template-file main.bicep \
  --parameters params.dev.json
```

This will:
1. Create DNS CNAME records (if using Azure DNS)
2. Create hostname binding without SSL
3. Verify domain ownership

### Step 4: Enable Managed SSL Certificate (Optional)

After Step 3 succeeds and hostname binding exists:

1. Update `infrastructure/params.dev.json`:
   ```json
   {
     "parameters": {
       "enableManagedCert": {
         "value": true
       }
     }
   }
   ```

2. Re-deploy infrastructure:
   ```bash
   az deployment group create \
     --resource-group mys-dev-mystira-rg-san \
     --template-file main.bicep \
     --parameters params.dev.json
   ```

3. Azure will automatically:
   - Create a free managed SSL certificate
   - Bind it to your custom domain
   - Handle renewal automatically

### Step 5: Verify Custom Domain Works

```bash
# Test HTTP (should redirect to HTTPS)
curl -I http://api.dev.mystira.app

# Test HTTPS
curl -I https://api.dev.mystira.app/health
```

## Important Notes

### Hostname Binding Creation Order
⚠️ **Critical**: The hostname binding must exist BEFORE enabling managed certificates.

The Bicep template follows this order:
1. Create hostname binding (SSL disabled)
2. Create managed certificate (references the binding)
3. Update hostname binding with SSL (SNI enabled)

### DNS Propagation Time
- DNS changes can take 5-60 minutes to propagate globally
- Wait for DNS to fully propagate before creating hostname binding
- Use `nslookup` or online DNS checkers to verify propagation

### Common Errors

**"Custom domain verification failed"**
- DNS CNAME isn't configured correctly
- DNS hasn't propagated yet
- CNAME points to wrong App Service hostname

**"Certificate creation failed"**
- Tried to create certificate before hostname binding exists
- Set `enableManagedCert: false` first, deploy, then set to `true`

**"No binding" persists after deployment**
- Check deployment logs in Azure Portal
- Verify `enableApiCustomDomain` is `true`
- Ensure App Service Plan isn't Free tier (custom domains require Basic or higher)

## Environments

### Dev Environment
- API: `api.dev.mystira.app`
- Admin API: `admin.dev.mystira.app`
- PWA: `dev.mystira.app`
- App Service: `mys-dev-mystira-api-san.azurewebsites.net`

### Staging Environment
- API: `api.staging.mystira.app`
- Admin API: `admin.staging.mystira.app`
- PWA: `staging.mystira.app`
- App Service: `mys-staging-mystira-api-san.azurewebsites.net`

### Production Environment
- API: `api.mystira.app`
- Admin API: `admin.mystira.app`
- PWA: `mystira.app`
- App Service: `mys-prod-mystira-api-san.azurewebsites.net`

## Automation

The infrastructure deployment automatically handles:
- ✅ DNS CNAME record creation (if using Azure DNS)
- ✅ Hostname binding creation
- ✅ SSL certificate provisioning (when enabled)
- ✅ Certificate renewal (automatic, free)
- ✅ App Service configuration updates

## Related Files

- `infrastructure/main.bicep` - Main infrastructure template
- `infrastructure/modules/app-service.bicep` - App Service configuration with custom domain support
- `infrastructure/modules/dns-zone.bicep` - DNS record management
- `infrastructure/params.dev.json` - Development environment parameters
- `infrastructure/params.staging.json` - Staging environment parameters
- `infrastructure/params.prod.json` - Production environment parameters

## Support

For issues with custom domain configuration:
1. Check Azure Portal deployment logs
2. Verify DNS configuration with `nslookup`
3. Review Bicep template parameters
4. Consult Azure App Service custom domain documentation
