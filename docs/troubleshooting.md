# Troubleshooting Guide

This guide helps you navigate common errors and issues in the Mystira.App project. Each section includes the error, what causes it, and step-by-step solutions.

## Quick Navigation

- [Azure Deployment Issues](#azure-deployment-issues)
- [Authentication Problems](#authentication-problems)
- [Database Errors](#database-errors)
- [Build & Development Issues](#build--development-issues)
- [Network & Connectivity](#network--connectivity)
- [Configuration Problems](#configuration-problems)

---

## Azure Deployment Issues

### AZURE_LOCATION_001: Region Not Available for Resource Type

**Error Message:**
```
LocationNotAvailableForResourceType: The provided location 'eastus' is not available for resource type 'Microsoft.Web/staticSites'.
```

**What This Means:**
Azure Static Web Apps (and some other resources) are only available in specific regions. Not all Azure services are available everywhere.

**Solutions:**

1. **Use a supported region:**
   ```bash
   # Static Web Apps supported regions:
   ./deploy-dev.sh -l westus2       # Western US
   ./deploy-dev.sh -l centralus     # Central US
   ./deploy-dev.sh -l eastus2       # Eastern US 2
   ./deploy-dev.sh -l westeurope    # Western Europe
   ./deploy-dev.sh -l eastasia      # East Asia
   ```

2. **Check available regions for any resource type:**
   ```bash
   # For Static Web Apps
   az provider show --namespace Microsoft.Web \
     --query "resourceTypes[?resourceType=='staticSites'].locations" -o tsv

   # For Cosmos DB
   az provider show --namespace Microsoft.DocumentDB \
     --query "resourceTypes[?resourceType=='databaseAccounts'].locations" -o tsv
   ```

**Related Links:**
- [Azure Products by Region](https://azure.microsoft.com/explore/global-infrastructure/products-by-region/)
- [Static Web Apps Regions](https://docs.microsoft.com/azure/static-web-apps/overview#regional-availability)

---

### AZURE_LOCATION_002: Resource Group Location Conflict

**Error Message:**
```
InvalidResourceGroupLocation: Invalid resource group location 'westeurope'.
The Resource group already exists in location 'eastus'.
```

**What This Means:**
Azure resource groups are tied to a specific region and cannot be moved. You're trying to create/update a resource group with a different location than where it was originally created.

**Solutions:**

1. **Option A - Use the existing location:**
   ```bash
   # Find the existing resource group's location
   az group show --name your-rg-name --query location -o tsv

   # Use that location in your deployment
   ./deploy-dev.sh -l <existing-location>
   ```

2. **Option B - Delete the old resource group:**
   ```bash
   # Delete the existing resource group (CAREFUL: deletes all resources inside!)
   az group delete --name your-rg-name --yes

   # Wait 1-2 minutes for Azure to process the deletion
   sleep 120

   # Now create with your preferred location
   ./deploy-dev.sh -l westeurope
   ```

3. **Option C - Use a different resource group name:**
   ```bash
   ./deploy-dev.sh -g mystira-app-westeurope-rg -l westeurope
   ```

**Pro Tip:** Name your resource groups with the region code to avoid confusion:
- `dev-eus2-rg-mystira` (East US 2)
- `dev-weu-rg-mystira` (West Europe)

---

### Deployment Timeout

**Error Message:**
```
Deployment timed out. The deployment operation is still running...
```

**Solutions:**

1. **Check deployment status:**
   ```bash
   az deployment group list --resource-group your-rg-name -o table
   ```

2. **View deployment operations:**
   ```bash
   az deployment operation group list \
     --resource-group your-rg-name \
     --name your-deployment-name
   ```

3. **Wait and retry:**
   Some Azure operations take 5-15 minutes. If it's in progress, just wait.

---

## Authentication Problems

### AUTH_001: Not Logged In

**Error Message:**
```
You are not logged in to Azure. Please run 'az login' first.
```

**Solutions:**

1. **Standard login (opens browser):**
   ```bash
   az login
   ```

2. **Device code login (for remote/headless environments):**
   ```bash
   az login --use-device-code
   ```

3. **Service principal login (for CI/CD):**
   ```bash
   az login --service-principal \
     --username $APP_ID \
     --password $CLIENT_SECRET \
     --tenant $TENANT_ID
   ```

---

### AUTH_002: Wrong Subscription

**Error Message:**
```
The subscription 'xxx' could not be found.
```

**Solutions:**

1. **List available subscriptions:**
   ```bash
   az account list --output table
   ```

2. **Switch to correct subscription:**
   ```bash
   az account set --subscription "Your Subscription Name"
   # or by ID
   az account set --subscription "22f9eb18-6553-4b7d-9451-47d0195085fe"
   ```

3. **Verify current subscription:**
   ```bash
   az account show
   ```

---

### JWT Token Invalid

**Error Message:**
```
InvalidOperationException: IDX10214: Audience validation failed.
```

**Solutions:**

1. **Check JWT settings in appsettings:**
   ```json
   {
     "JwtSettings": {
       "SecretKey": "your-32-character-minimum-secret-key-here",
       "Issuer": "https://mystira.app",
       "Audience": "https://mystira.app",
       "ExpirationInMinutes": 60
     }
   }
   ```

2. **Generate a proper secret key:**
   ```bash
   openssl rand -base64 32
   ```

3. **For local development, use user secrets:**
   ```bash
   cd src/Mystira.App.Api
   dotnet user-secrets set "JwtSettings:SecretKey" "$(openssl rand -base64 32)"
   ```

---

## Database Errors

### DB_001: Cosmos DB Connection Failed

**Error Message:**
```
CosmosException: Request failed with status code ServiceUnavailable
```

**Solutions:**

1. **Verify Cosmos account exists:**
   ```bash
   az cosmosdb list --resource-group your-rg-name -o table
   ```

2. **Check connection string:**
   ```bash
   az cosmosdb keys list \
     --name your-cosmos-account \
     --resource-group your-rg-name \
     --type connection-strings
   ```

3. **For local development, use in-memory database:**
   ```json
   // appsettings.Development.json
   {
     "Database": {
       "UseInMemoryDatabase": true
     }
   }
   ```

---

### DB_002: Partition Key Error

**Error Message:**
```
PartitionKey extracted from document doesn't match the one specified in the header.
```

**Solutions:**

1. **Ensure partition key is set on entity:**
   ```csharp
   public class MyEntity : Entity
   {
       // This must match your Cosmos container's partition key path
       [JsonProperty("partitionKey")]
       public string PartitionKey => this.GetPartitionKey();
   }
   ```

2. **Check container configuration matches entity:**
   ```bash
   az cosmosdb sql container show \
     --account-name your-account \
     --database-name mystira-db \
     --name your-container \
     --resource-group your-rg
   ```

---

## Build & Development Issues

### BUILD_001: npm Install Failed

**Error Message:**
```
npm ERR! code ERESOLVE
npm ERR! ERESOLVE unable to resolve dependency tree
```

**Solutions:**

1. **Clear npm cache and reinstall:**
   ```bash
   rm -rf node_modules package-lock.json
   npm cache clean --force
   npm install
   ```

2. **Use legacy peer deps (if needed):**
   ```bash
   npm install --legacy-peer-deps
   ```

3. **Check Node.js version:**
   ```bash
   node --version  # Should be 18.x or higher

   # If wrong version, use nvm:
   nvm install 18
   nvm use 18
   ```

---

### BUILD_002: Rust/Tauri Build Failed

**Error Message:**
```
error: could not find `Cargo.toml`
# or
cargo: command not found
```

**Solutions:**

1. **Install Rust:**
   ```bash
   curl --proto '=https' --tlsv1.2 -sSf https://sh.rustup.rs | sh
   source $HOME/.cargo/env
   ```

2. **Update Rust:**
   ```bash
   rustup update stable
   ```

3. **Install Tauri dependencies (Ubuntu/Debian):**
   ```bash
   sudo apt update
   sudo apt install libwebkit2gtk-4.1-dev build-essential curl wget \
     file libssl-dev libayatana-appindicator3-dev librsvg2-dev
   ```

4. **Clean and rebuild:**
   ```bash
   cd tools/Mystira.DevHub
   cargo clean
   npm run tauri:build
   ```

---

### BUILD_003: .NET Build Failed

**Error Message:**
```
error NU1301: Unable to load the service index for source
```

**Solutions:**

1. **Clear NuGet cache:**
   ```bash
   dotnet nuget locals all --clear
   ```

2. **Restore packages:**
   ```bash
   dotnet restore Mystira.sln
   ```

3. **Check NuGet.config:**
   ```bash
   cat NuGet.config
   # Should have nuget.org as a source
   ```

4. **Verify .NET SDK version:**
   ```bash
   dotnet --list-sdks
   # Should show 9.x
   ```

---

## Network & Connectivity

### NET_001: Connection Refused

**Error Message:**
```
System.Net.Http.HttpRequestException: Connection refused
```

**Solutions:**

1. **For local development, ensure services are running:**
   ```bash
   # Start all services
   ./scripts/start-all.ps1

   # Or start individually
   ./scripts/start-api.ps1
   ./scripts/start-pwa.ps1
   ```

2. **Check ports aren't in use:**
   ```bash
   # Check if port 5000 is in use
   lsof -i :5000

   # Kill process if needed
   kill -9 <PID>
   ```

3. **Verify URLs in configuration:**
   ```json
   // appsettings.Development.json
   {
     "ApiBaseUrl": "https://localhost:5001",
     "PwaUrl": "https://localhost:7001"
   }
   ```

---

### NET_002: CORS Error

**Error Message:**
```
Access to XMLHttpRequest at '...' has been blocked by CORS policy
```

**Solutions:**

1. **Check CORS configuration in API:**
   ```csharp
   // Program.cs
   builder.Services.AddCors(options =>
   {
       options.AddPolicy("Development", policy =>
       {
           policy.WithOrigins(
               "https://localhost:7001",
               "http://localhost:5173"  // DevHub
           )
           .AllowAnyHeader()
           .AllowAnyMethod();
       });
   });
   ```

2. **Verify your origin is in the allowed list in appsettings:**
   ```json
   {
     "Cors": {
       "AllowedOrigins": [
         "https://localhost:7001",
         "https://mystira.app"
       ]
     }
   }
   ```

---

## Configuration Problems

### CONFIG_001: Missing Configuration Value

**Error Message:**
```
System.InvalidOperationException: Configuration value 'XYZ' not found
```

**Solutions:**

1. **Check appsettings.json has the value:**
   ```json
   {
     "XYZ": "your-value-here"
   }
   ```

2. **For secrets, use user-secrets:**
   ```bash
   cd src/Mystira.App.Api
   dotnet user-secrets init  # if not already initialized
   dotnet user-secrets set "ConnectionStrings:CosmosDb" "your-connection-string"
   ```

3. **Check environment variables:**
   ```bash
   # Linux/Mac
   export XYZ="your-value"

   # Or in .env file (if using dotenv)
   XYZ=your-value
   ```

---

## Getting More Help

If you're still stuck:

1. **Check Azure Status:** https://status.azure.com
2. **Review deployment logs in Azure Portal**
3. **Search existing issues:** Check the project's GitHub issues
4. **Enable verbose logging:**
   ```bash
   az ... --debug
   dotnet run --verbosity detailed
   ```

### Useful Diagnostic Commands

```bash
# Azure account info
az account show

# List all resources in a resource group
az resource list --resource-group your-rg-name -o table

# View deployment history
az deployment group list --resource-group your-rg-name -o table

# Check Azure CLI version
az version

# Check .NET SDK version
dotnet --info

# Check Node.js version
node --version && npm --version

# View running ports
netstat -tlnp 2>/dev/null || ss -tlnp
```
