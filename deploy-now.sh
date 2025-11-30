#!/bin/bash
# deploy-now.sh - No-nonsense deployment script
# Just run: ./deploy-now.sh

set -e

RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

echo "================================================"
echo "  Mystira.App - Quick Deploy"
echo "================================================"

# Config
RG="dev-euw-rg-mystira-app"
SUB="22f9eb18-6553-4b7d-9451-47d0195085fe"

# Valid regions for ALL Azure services we use (App Service, Cosmos, Storage, Static Web Apps)
# Using westeurope because it supports everything
LOCATION="westeurope"

# Step 1: Check Azure CLI
if ! command -v az &> /dev/null; then
    echo -e "${RED}Azure CLI not installed.${NC}"
    echo "Install: https://docs.microsoft.com/cli/azure/install-azure-cli"
    exit 1
fi

# Step 2: Login check
echo -n "Checking Azure login... "
if ! az account show &>/dev/null; then
    echo -e "${YELLOW}not logged in${NC}"
    echo ""
    echo "Login with device code (works in any terminal):"
    az login --use-device-code
else
    ACCOUNT=$(az account show --query name -o tsv)
    echo -e "${GREEN}OK${NC} ($ACCOUNT)"
fi

# Step 3: Set subscription
echo -n "Setting subscription... "
az account set --subscription "$SUB" 2>/dev/null || {
    echo -e "${RED}Failed${NC}"
    echo "Available subscriptions:"
    az account list --output table
    echo ""
    echo "Set with: az account set --subscription \"<name or id>\""
    exit 1
}
echo -e "${GREEN}OK${NC}"

# Step 4: Handle resource group
echo -n "Checking resource group... "
EXISTING_LOCATION=$(az group show --name "$RG" --query location -o tsv 2>/dev/null || echo "")

if [ -n "$EXISTING_LOCATION" ]; then
    if [ "$EXISTING_LOCATION" != "$LOCATION" ]; then
        echo -e "${YELLOW}exists in $EXISTING_LOCATION${NC}"
        echo ""
        echo "Resource group is in $EXISTING_LOCATION but we want $LOCATION."
        echo ""
        echo "Options:"
        echo "  1) Use existing location ($EXISTING_LOCATION)"
        echo "  2) Delete resource group and create new"
        echo "  3) Cancel"
        read -p "Choice [1]: " choice
        choice=${choice:-1}

        case $choice in
            1)
                LOCATION="$EXISTING_LOCATION"
                echo "Using location: $LOCATION"
                ;;
            2)
                echo "Deleting resource group..."
                az group delete --name "$RG" --yes --no-wait
                echo "Waiting 60s for deletion..."
                sleep 60
                echo "Creating resource group in $LOCATION..."
                az group create --name "$RG" --location "$LOCATION" --output none
                ;;
            *)
                echo "Cancelled."
                exit 0
                ;;
        esac
    else
        echo -e "${GREEN}OK${NC} ($EXISTING_LOCATION)"
    fi
else
    echo -e "${YELLOW}creating${NC}"
    az group create --name "$RG" --location "$LOCATION" --output none
    echo -e "Created in $LOCATION"
fi

# Step 5: Generate JWT secret
JWT_SECRET=$(openssl rand -base64 32)

# Step 6: Find bicep template
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
BICEP_FILE=""

# Try multiple locations
for path in \
    "$SCRIPT_DIR/infrastructure/dev/main.bicep" \
    "$SCRIPT_DIR/src/Mystira.App.Infrastructure.Azure/Deployment/dev/main.bicep" \
    "./infrastructure/dev/main.bicep" \
    "./src/Mystira.App.Infrastructure.Azure/Deployment/dev/main.bicep"; do
    if [ -f "$path" ]; then
        BICEP_FILE="$path"
        break
    fi
done

if [ -z "$BICEP_FILE" ]; then
    echo -e "${RED}Can't find main.bicep${NC}"
    echo "Run this from the repo root."
    exit 1
fi

echo "Using template: $BICEP_FILE"

# Step 7: Deploy
echo ""
echo "Deploying infrastructure..."
echo "This takes 5-10 minutes. Go grab coffee."
echo ""

DEPLOY_NAME="mystira-$(date +%Y%m%d-%H%M%S)"

if az deployment group create \
    --resource-group "$RG" \
    --template-file "$BICEP_FILE" \
    --parameters environment=dev \
                 location="$LOCATION" \
                 jwtSecretKey="$JWT_SECRET" \
    --mode Incremental \
    --name "$DEPLOY_NAME" \
    --output table; then

    echo ""
    echo -e "${GREEN}================================================${NC}"
    echo -e "${GREEN}  Deployment successful!${NC}"
    echo -e "${GREEN}================================================${NC}"
    echo ""

    # Get outputs
    API_URL=$(az deployment group show --resource-group "$RG" --name "$DEPLOY_NAME" \
        --query "properties.outputs.apiAppServiceUrl.value" -o tsv 2>/dev/null || echo "")
    ADMIN_URL=$(az deployment group show --resource-group "$RG" --name "$DEPLOY_NAME" \
        --query "properties.outputs.adminApiAppServiceUrl.value" -o tsv 2>/dev/null || echo "")

    echo "Resources deployed to: $RG"
    echo "Location: $LOCATION"
    [ -n "$API_URL" ] && echo "API URL: $API_URL"
    [ -n "$ADMIN_URL" ] && echo "Admin API URL: $ADMIN_URL"
    echo ""
    echo "JWT Secret (save this!):"
    echo "$JWT_SECRET"
    echo ""
    echo "Next: Deploy your code with GitHub Actions or:"
    echo "  az webapp deploy --resource-group $RG --name dev-euw-app-mystira-api --src-path <your-zip>"
else
    echo ""
    echo -e "${RED}================================================${NC}"
    echo -e "${RED}  Deployment failed${NC}"
    echo -e "${RED}================================================${NC}"
    echo ""
    echo "Common fixes:"
    echo ""
    echo "1. Location issue? Try: $0 with a different region"
    echo "   Edit this script and change LOCATION to one of:"
    echo "   westeurope, eastus2, westus2, centralus"
    echo ""
    echo "2. Quota issue? Check usage:"
    echo "   az vm list-usage --location $LOCATION -o table"
    echo ""
    echo "3. Auth issue? Re-login:"
    echo "   az login"
    echo ""
    echo "4. See full error:"
    echo "   az deployment group list --resource-group $RG -o table"
    echo "   az deployment operation group list --resource-group $RG --name $DEPLOY_NAME"
    exit 1
fi
