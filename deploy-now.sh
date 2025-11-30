#!/bin/bash
# deploy-now.sh - No-nonsense deployment script
# Just run: ./deploy-now.sh [region]
# Example: ./deploy-now.sh eastus2

set -e

RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
NC='\033[0m'

echo "================================================"
echo "  Mystira.App - Quick Deploy"
echo "================================================"

# Config
SUB="22f9eb18-6553-4b7d-9451-47d0195085fe"

# Fallback regions - all support Cosmos, App Service, Storage, Static Web Apps
# Ordered by reliability/capacity
REGIONS=("eastus2" "westus2" "centralus" "westeurope" "northeurope" "eastasia")

# Use provided region or default to first in list
LOCATION="${1:-${REGIONS[0]}}"

# Resource group name includes region code
get_rg_name() {
    local loc="$1"
    case "$loc" in
        eastus2)     echo "dev-eus2-rg-mystira-app" ;;
        westus2)     echo "dev-wus2-rg-mystira-app" ;;
        centralus)   echo "dev-cus-rg-mystira-app" ;;
        westeurope)  echo "dev-euw-rg-mystira-app" ;;
        northeurope) echo "dev-eun-rg-mystira-app" ;;
        eastasia)    echo "dev-ea-rg-mystira-app" ;;
        *)           echo "dev-${loc:0:4}-rg-mystira-app" ;;
    esac
}

RG=$(get_rg_name "$LOCATION")

echo ""
echo -e "Region: ${CYAN}$LOCATION${NC}"
echo -e "Resource Group: ${CYAN}$RG${NC}"
echo -e "Fallbacks: ${REGIONS[*]}"
echo ""

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

# Step 7: Deploy with auto-fallback
deploy_to_region() {
    local region="$1"
    local rg=$(get_rg_name "$region")
    local deploy_name="mystira-$(date +%Y%m%d-%H%M%S)"

    echo ""
    echo -e "Deploying to ${CYAN}$region${NC}..."
    echo "Resource group: $rg"
    echo ""

    # Create/update resource group for this region
    az group create --name "$rg" --location "$region" --output none 2>/dev/null || true

    # Try deployment
    local output
    output=$(az deployment group create \
        --resource-group "$rg" \
        --template-file "$BICEP_FILE" \
        --parameters environment=dev \
                     location="$region" \
                     jwtSecretKey="$JWT_SECRET" \
        --mode Incremental \
        --name "$deploy_name" \
        --output json 2>&1)
    local exit_code=$?

    if [ $exit_code -eq 0 ]; then
        echo ""
        echo -e "${GREEN}================================================${NC}"
        echo -e "${GREEN}  Deployment successful!${NC}"
        echo -e "${GREEN}================================================${NC}"
        echo ""

        # Get outputs
        API_URL=$(az deployment group show --resource-group "$rg" --name "$deploy_name" \
            --query "properties.outputs.apiAppServiceUrl.value" -o tsv 2>/dev/null || echo "")
        ADMIN_URL=$(az deployment group show --resource-group "$rg" --name "$deploy_name" \
            --query "properties.outputs.adminApiAppServiceUrl.value" -o tsv 2>/dev/null || echo "")

        echo "Resources deployed to: $rg"
        echo "Location: $region"
        [ -n "$API_URL" ] && echo "API URL: $API_URL"
        [ -n "$ADMIN_URL" ] && echo "Admin API URL: $ADMIN_URL"
        echo ""
        echo "JWT Secret (save this!):"
        echo "$JWT_SECRET"
        echo ""
        return 0
    else
        echo -e "${RED}Failed in $region${NC}"
        # Check if it's a region/service availability issue
        if echo "$output" | grep -qi "LocationNotAvailable\|ServiceUnavailable\|capacity\|quota\|not available"; then
            echo -e "${YELLOW}Region issue detected - will try fallback${NC}"
            return 1
        else
            # Other error - show it
            echo "$output" | head -20
            return 2
        fi
    fi
}

echo ""
echo "Deploying infrastructure..."
echo "This takes 5-10 minutes. Will auto-fallback if region fails."
echo ""

# Try primary region first
if deploy_to_region "$LOCATION"; then
    exit 0
fi

# Primary failed - try fallbacks
echo ""
echo -e "${YELLOW}Primary region failed. Trying fallbacks...${NC}"

for fallback in "${REGIONS[@]}"; do
    # Skip if same as primary
    [ "$fallback" = "$LOCATION" ] && continue

    echo ""
    echo -e "Trying fallback: ${CYAN}$fallback${NC}"

    if deploy_to_region "$fallback"; then
        exit 0
    fi

    result=$?
    if [ $result -eq 2 ]; then
        # Non-region error, don't keep trying
        echo -e "${RED}Non-recoverable error. Stopping.${NC}"
        break
    fi
done

# All regions failed
echo ""
echo -e "${RED}================================================${NC}"
echo -e "${RED}  All regions failed${NC}"
echo -e "${RED}================================================${NC}"
echo ""
echo "Tried: ${REGIONS[*]}"
echo ""
echo "Troubleshooting:"
echo "  1. Check Azure status: https://status.azure.com"
echo "  2. Re-authenticate: az login"
echo "  3. Check quota: az vm list-usage --location eastus2 -o table"
echo "  4. View errors: az deployment group list -g dev-eus2-rg-mystira-app -o table"
exit 1
