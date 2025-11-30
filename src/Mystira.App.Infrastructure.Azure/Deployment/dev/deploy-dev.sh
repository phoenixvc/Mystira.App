#!/bin/bash

# Azure Infrastructure Deployment Script for Mystira.App
# Enhanced with troubleshooting support for common Azure errors

# Source troubleshooting utilities
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
if [ -f "$SCRIPT_DIR/../common/troubleshoot.sh" ]; then
    source "$SCRIPT_DIR/../common/troubleshoot.sh"
    TROUBLESHOOT_ENABLED=true
else
    TROUBLESHOOT_ENABLED=false
    # Fallback colored output
    RED='\033[0;31m'
    GREEN='\033[0;32m'
    YELLOW='\033[1;33m'
    NC='\033[0m'
fi

# Don't exit immediately on error - we want to handle them gracefully
set -o pipefail

# ⚠️ CONFIGURATION REQUIRED: Update these default values for your deployment
# Default values - ⚠️ UPDATE: Change these defaults to match your environment
ENVIRONMENT="dev" # ⚠️ UPDATE: Set to 'dev', 'staging', or 'prod'
RESOURCE_GROUP="dev-euw-rg-mystira-app" # ⚠️ UPDATE: Set your resource group name (standardized format: {env}-{location}-rg-{app})
LOCATION="westeurope" # ⚠️ UPDATE: Set your preferred Azure region
SUBSCRIPTION_ID="22f9eb18-6553-4b7d-9451-47d0195085fe" # ⚠️ UPDATE: Phoenix Azure Sponsorship subscription ID

# Supported regions for Static Web Apps (for quick reference)
STATIC_WEB_APP_REGIONS="westus2|centralus|eastus2|westeurope|eastasia"

# Parse command line arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        -e|--environment)
            ENVIRONMENT="$2"
            shift 2
            ;;
        -g|--resource-group)
            RESOURCE_GROUP="$2"
            shift 2
            ;;
        -l|--location)
            LOCATION="$2"
            shift 2
            ;;
        -s|--subscription)
            SUBSCRIPTION_ID="$2"
            shift 2
            ;;
        -h|--help)
            echo "Usage: deploy.sh [OPTIONS]"
            echo ""
            echo "Options:"
            echo "  -e, --environment    Environment (dev, staging, prod) [default: dev]"
            echo "  -g, --resource-group Resource group name [required]"
            echo "  -l, --location       Azure location [default: westeurope]"
            echo "  -s, --subscription   Azure subscription ID [optional]"
            echo "  -h, --help          Show this help message"
            echo ""
            echo "Supported regions for Static Web Apps:"
            echo "  westus2, centralus, eastus2, westeurope, eastasia"
            echo ""
            echo "Examples:"
            echo "  ./deploy-dev.sh                           # Deploy with defaults"
            echo "  ./deploy-dev.sh -l westus2                # Use westus2 region"
            echo "  ./deploy-dev.sh -g my-rg -l eastus2       # Custom RG and location"
            echo ""
            echo "Troubleshooting:"
            echo "  If deployment fails, the script will provide specific guidance"
            echo "  based on the error type. Common issues include:"
            echo "    - Region not available: Use a different -l location"
            echo "    - Resource group conflict: Delete old RG or use existing location"
            echo "    - Auth errors: Run 'az login' to refresh credentials"
            exit 0
            ;;
        *)
            echo "Unknown option: $1"
            exit 1
            ;;
    esac
done

# Validate required parameters
if [ -z "$RESOURCE_GROUP" ]; then
    echo "Error: Resource group is required. Use -g or --resource-group to specify."
    exit 1
fi

# Print header
if [ "$TROUBLESHOOT_ENABLED" = true ]; then
    print_header "Mystira.App Azure Deployment"
else
    echo "🚀 Deploying Mystira.App Azure Infrastructure"
fi

echo "   Environment: $ENVIRONMENT"
echo "   Resource Group: $RESOURCE_GROUP"
echo "   Location: $LOCATION"

# Set subscription if provided
if [ -n "$SUBSCRIPTION_ID" ]; then
    echo "   Subscription: $SUBSCRIPTION_ID"
    az account set --subscription "$SUBSCRIPTION_ID"
fi

# Check prerequisites with helpful output
if [ "$TROUBLESHOOT_ENABLED" = true ]; then
    if ! check_prerequisites; then
        echo -e "${RED}❌ Prerequisites check failed. Please resolve the issues above.${NC}"
        exit 1
    fi
else
    # Check if user is logged in to Azure
    if ! az account show &>/dev/null; then
        echo "❌ You are not logged in to Azure. Please run 'az login' first."
        exit 1
    fi
fi

# Create resource group if it doesn't exist (with error handling)
echo "📦 Creating resource group if it doesn't exist..."
rg_output=$(az group create --name "$RESOURCE_GROUP" --location "$LOCATION" 2>&1)
rg_exit_code=$?

if [ $rg_exit_code -ne 0 ]; then
    if [ "$TROUBLESHOOT_ENABLED" = true ]; then
        print_error_with_help "Resource group creation failed" "$rg_output"
    else
        echo -e "${RED}❌ Failed to create resource group${NC}"
        echo "$rg_output"
    fi
    exit 1
fi
echo -e "${GREEN}✓${NC} Resource group ready"

# Generate JWT secret key
JWT_SECRET=$(openssl rand -base64 32)

# Path to bicep template
BICEP_TEMPLATE="main.bicep"

# Check if bicep file exists
if [ ! -f "$BICEP_TEMPLATE" ]; then
    echo "❌ Bicep template not found at: $BICEP_TEMPLATE"
    exit 1
fi

# Deploy infrastructure (Incremental mode - SAFE: won't delete resources not in template)
echo "🏗️  Deploying Azure resources (Incremental mode - safe, won't delete existing resources)..."
DEPLOYMENT_NAME="mystira-app-$ENVIRONMENT-$(date +%Y%m%d-%H%M%S)"

# ⚠️ SAFETY: Using --mode Incremental (default) to prevent accidental resource deletion
# Incremental mode only creates/updates resources in the template, never deletes existing ones

deploy_output=$(az deployment group create \
    --resource-group "$RESOURCE_GROUP" \
    --template-file "$BICEP_TEMPLATE" \
    --parameters environment="$ENVIRONMENT" \
               location="$LOCATION" \
               jwtSecretKey="$JWT_SECRET" \
               deployStorage=true \
               deployCosmos=true \
               deployAppService=true \
    --mode Incremental \
    --name "$DEPLOYMENT_NAME" \
    --output table 2>&1)
deploy_exit_code=$?

if [ $deploy_exit_code -ne 0 ]; then
    echo ""
    if [ "$TROUBLESHOOT_ENABLED" = true ]; then
        print_error_with_help "Infrastructure deployment failed" "$deploy_output"

        # Offer recovery options
        echo -e "\n${YELLOW}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
        show_recovery_menu
        recovery_choice=$?

        if [ $recovery_choice -eq 0 ]; then
            echo "Retrying deployment with location: $LOCATION"
            exec "$0" -e "$ENVIRONMENT" -g "$RESOURCE_GROUP" -l "$LOCATION" -s "$SUBSCRIPTION_ID"
        fi
    else
        echo -e "${RED}❌ Deployment failed${NC}"
        echo "$deploy_output"

        # Still provide basic help even without troubleshoot.sh
        if [[ "$deploy_output" == *"LocationNotAvailable"* ]]; then
            echo ""
            echo -e "${YELLOW}💡 Tip: This region may not support all resource types.${NC}"
            echo -e "   Try a different location: ${GREEN}./deploy-dev.sh -l westus2${NC}"
        elif [[ "$deploy_output" == *"InvalidResourceGroupLocation"* ]]; then
            echo ""
            echo -e "${YELLOW}💡 Tip: Resource group exists in different location.${NC}"
            echo -e "   Option 1: Delete existing: ${GREEN}az group delete --name $RESOURCE_GROUP --yes${NC}"
            echo -e "   Option 2: Use existing location"
        fi
    fi
    exit 1
fi

echo "$deploy_output"
echo -e "${GREEN}✓${NC} Infrastructure deployment completed"

# Get deployment outputs
echo "📋 Getting deployment outputs..."
APP_SERVICE_URL=$(az deployment group show \
    --resource-group "$RESOURCE_GROUP" \
    --name "$DEPLOYMENT_NAME" \
    --query "properties.outputs.appServiceUrl.value" \
    --output tsv)

STORAGE_ACCOUNT=$(az deployment group show \
    --resource-group "$RESOURCE_GROUP" \
    --name "$DEPLOYMENT_NAME" \
    --query "properties.outputs.storageAccountName.value" \
    --output tsv)

COSMOS_ACCOUNT=$(az deployment group show \
    --resource-group "$RESOURCE_GROUP" \
    --name "$DEPLOYMENT_NAME" \
    --query "properties.outputs.cosmosDbAccountName.value" \
    --output tsv)

echo "✅ Deployment completed successfully!"
echo ""
echo "📊 Resource Summary:"
echo "   App Service URL: $APP_SERVICE_URL"
echo "   Storage Account: $STORAGE_ACCOUNT"
echo "   Cosmos DB Account: $COSMOS_ACCOUNT"
echo ""
echo "🔑 Configuration:"
echo "   JWT Secret: $JWT_SECRET"
echo ""
echo "🔗 Next Steps:"
echo "   1. Configure your application with the connection strings"
echo "   2. Deploy your application code to the App Service"
echo "   3. Test the health endpoints: $APP_SERVICE_URL/health"

# Save configuration to file
CONFIG_FILE="mystira-app-$ENVIRONMENT-config.json"
cat > "$CONFIG_FILE" << EOF
{
  "environment": "$ENVIRONMENT",
  "resourceGroup": "$RESOURCE_GROUP",
  "location": "$LOCATION",
  "appServiceUrl": "$APP_SERVICE_URL",
  "storageAccount": "$STORAGE_ACCOUNT",
  "cosmosAccount": "$COSMOS_ACCOUNT",
  "jwtSecret": "$JWT_SECRET",
  "deploymentName": "$DEPLOYMENT_NAME",
  "deployedAt": "$(date -u +%Y-%m-%dT%H:%M:%SZ)"
}
EOF

echo "💾 Configuration saved to: $CONFIG_FILE"