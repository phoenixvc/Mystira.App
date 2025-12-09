#!/bin/bash
# CORS Configuration Updater for Staging SWA Migration
# This script extracts the Staging SWA URL and updates API CORS configurations

set -e

echo "======================================"
echo "Staging SWA CORS Configuration Update"
echo "======================================"
echo ""

# Check if Azure CLI is installed
if ! command -v az &> /dev/null; then
    echo "❌ Azure CLI not found. Please install: https://learn.microsoft.com/cli/azure/install-azure-cli"
    exit 1
fi

# Configuration
RESOURCE_GROUP="${RESOURCE_GROUP:-rg-mystira-app}"
SWA_NAME="${SWA_NAME:-mystira-app-staging-swa}"
API_PROJECT_PATH="${API_PROJECT_PATH:-src/Mystira.App.Api}"
ADMIN_API_PROJECT_PATH="${ADMIN_API_PROJECT_PATH:-src/Mystira.App.Admin.Api}"

echo "📋 Configuration:"
echo "  Resource Group: $RESOURCE_GROUP"
echo "  SWA Name: $SWA_NAME"
echo "  API Project: $API_PROJECT_PATH"
echo "  Admin API Project: $ADMIN_API_PROJECT_PATH"
echo ""

# Check if logged in to Azure
echo "🔐 Checking Azure login..."
if ! az account show &> /dev/null; then
    echo "❌ Not logged in to Azure. Please run: az login"
    exit 1
fi
echo "✅ Azure login verified"
echo ""

# Get SWA hostname
echo "🌐 Fetching Staging SWA URL..."
SWA_HOSTNAME=$(az staticwebapp show \
    --name "$SWA_NAME" \
    --resource-group "$RESOURCE_GROUP" \
    --query "defaultHostname" \
    --output tsv 2>/dev/null)

if [ -z "$SWA_HOSTNAME" ]; then
    echo "❌ Could not find Static Web App: $SWA_NAME in resource group: $RESOURCE_GROUP"
    echo "   Please ensure the SWA resource has been created."
    exit 1
fi

STAGING_URL="https://$SWA_HOSTNAME"
echo "✅ Found Staging SWA URL: $STAGING_URL"
echo ""

# Function to update appsettings.json
update_appsettings() {
    local file_path=$1
    local description=$2
    
    if [ ! -f "$file_path" ]; then
        echo "⚠️  File not found: $file_path (skipping)"
        return
    fi
    
    echo "📝 Updating $description..."
    echo "   File: $file_path"
    
    # Check if file has CorsSettings section
    if ! grep -q "CorsSettings" "$file_path"; then
        echo "⚠️  No CorsSettings found in $file_path (skipping)"
        return
    fi
    
    # Check if Staging URL already present
    if grep -q "$STAGING_URL" "$file_path"; then
        echo "✅ Staging URL already present in CORS settings"
        return
    fi
    
    # Backup original file
    cp "$file_path" "${file_path}.backup"
    echo "   Created backup: ${file_path}.backup"
    
    # Get current AllowedOrigins value
    CURRENT_ORIGINS=$(grep -A1 "AllowedOrigins" "$file_path" | grep -v "AllowedOrigins" | sed 's/[",]//g' | xargs)
    
    if [ -z "$CURRENT_ORIGINS" ]; then
        # If empty, just add the Staging URL
        NEW_ORIGINS="$STAGING_URL"
    else
        # Append Staging URL to existing origins
        NEW_ORIGINS="$CURRENT_ORIGINS,$STAGING_URL"
    fi
    
    # Update the file using sed
    sed -i.tmp "s|\"AllowedOrigins\":.*|\"AllowedOrigins\": \"$NEW_ORIGINS\"|g" "$file_path"
    rm -f "${file_path}.tmp"
    
    echo "✅ Updated CORS configuration"
    echo "   New AllowedOrigins: $NEW_ORIGINS"
}

# Update API configurations
echo "🔧 Updating API CORS Configurations..."
echo ""

# Main API - appsettings.json
update_appsettings "$API_PROJECT_PATH/appsettings.json" "Main API (appsettings.json)"

# Main API - appsettings.Development.json
update_appsettings "$API_PROJECT_PATH/appsettings.Development.json" "Main API (appsettings.Development.json)"

# Main API - appsettings.Staging.json (if exists)
update_appsettings "$API_PROJECT_PATH/appsettings.Staging.json" "Main API (appsettings.Staging.json)"

# Admin API - appsettings.json
update_appsettings "$ADMIN_API_PROJECT_PATH/appsettings.json" "Admin API (appsettings.json)"

# Admin API - appsettings.Development.json
update_appsettings "$ADMIN_API_PROJECT_PATH/appsettings.Development.json" "Admin API (appsettings.Development.json)"

# Admin API - appsettings.Staging.json (if exists)
update_appsettings "$ADMIN_API_PROJECT_PATH/appsettings.Staging.json" "Admin API (appsettings.Staging.json)"

echo ""
echo "======================================"
echo "✅ CORS Configuration Update Complete"
echo "======================================"
echo ""
echo "📋 Next Steps:"
echo "1. Review the changes in your appsettings files"
echo "2. Commit the changes: git add src/*/appsettings*.json"
echo "3. Deploy the APIs to apply the new CORS configuration"
echo "4. Test API calls from Staging SWA URL"
echo ""
echo "🔄 To restore backups if needed:"
echo "   find src -name 'appsettings*.json.backup' -exec bash -c 'mv \"\$1\" \"\${1%.backup}\"' _ {} \;"
echo ""
