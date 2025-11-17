#!/bin/bash

# Azure Infrastructure Deployment Script for Mystira.App
set -e

# ⚠️ CONFIGURATION REQUIRED: Update these default values for your deployment
# Default values - ⚠️ UPDATE: Change these defaults to match your environment
ENVIRONMENT="dev" # ⚠️ UPDATE: Set to 'dev', 'staging', or 'prod'
RESOURCE_GROUP="dev-wus-app-mystira" # ⚠️ UPDATE: Set your resource group name (e.g., 'myapp-rg-dev')
LOCATION="westus" # ⚠️ UPDATE: Set your preferred Azure region
SUBSCRIPTION_ID="991fddfd-dba0-4154-88fb-00abc2108e69" # ⚠️ UPDATE: Set your Azure subscription ID

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
            echo "Options:"
            echo "  -e, --environment    Environment (dev, staging, prod) [default: dev]"
            echo "  -g, --resource-group Resource group name [required]"
            echo "  -l, --location       Azure location [default: westus]"
            echo "  -s, --subscription   Azure subscription ID [optional]"
            echo "  -h, --help          Show this help message"
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

echo "🚀 Deploying Mystira.App Azure Infrastructure"
echo "   Environment: $ENVIRONMENT"
echo "   Resource Group: $RESOURCE_GROUP"
echo "   Location: $LOCATION"

# Set subscription if provided
if [ -n "$SUBSCRIPTION_ID" ]; then
    echo "   Subscription: $SUBSCRIPTION_ID"
    az account set --subscription "$SUBSCRIPTION_ID"
fi

# Check if user is logged in to Azure
if ! az account show &>/dev/null; then
    echo "❌ You are not logged in to Azure. Please run 'az login' first."
    exit 1
fi

# Create resource group if it doesn't exist
echo "📦 Creating resource group if it doesn't exist..."
az group create --name "$RESOURCE_GROUP" --location "$LOCATION" --output none

# Generate JWT secret key
JWT_SECRET=$(openssl rand -base64 32)

# Path to bicep template
BICEP_TEMPLATE="main.bicep"

# Check if bicep file exists
if [ ! -f "$BICEP_TEMPLATE" ]; then
    echo "❌ Bicep template not found at: $BICEP_TEMPLATE"
    exit 1
fi

# Deploy infrastructure
echo "🏗️  Deploying Azure resources..."
DEPLOYMENT_NAME="mystira-app-$ENVIRONMENT-$(date +%Y%m%d-%H%M%S)"

az deployment group create \
    --resource-group "$RESOURCE_GROUP" \
    --template-file "$BICEP_TEMPLATE" \
    --parameters environment="$ENVIRONMENT" \
               location="$LOCATION" \
               jwtSecretKey="$JWT_SECRET" \
    --name "$DEPLOYMENT_NAME" \
    --output table

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