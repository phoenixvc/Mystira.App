#!/bin/bash

# Troubleshooting Helper for Azure Deployments
# Source this file in your deployment scripts: source "$(dirname "$0")/common/troubleshoot.sh"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color
BOLD='\033[1m'

# Print a styled header
print_header() {
    echo -e "\n${CYAN}╔═══════════════════════════════════════════════════════════════╗${NC}"
    echo -e "${CYAN}║${NC}  ${BOLD}$1${NC}"
    echo -e "${CYAN}╚═══════════════════════════════════════════════════════════════╝${NC}\n"
}

# Print an error with troubleshooting suggestions
print_error_with_help() {
    local error_message="$1"
    local error_code="$2"

    echo -e "\n${RED}╔═══════════════════════════════════════════════════════════════╗${NC}"
    echo -e "${RED}║${NC}  ${BOLD}❌ ERROR: ${error_message}${NC}"
    echo -e "${RED}╚═══════════════════════════════════════════════════════════════╝${NC}"

    # Match error patterns and provide specific guidance
    case "$error_code" in
        "LocationNotAvailableForResourceType"|*"LocationNotAvailable"*)
            print_location_error_help
            ;;
        "InvalidResourceGroupLocation"|*"already exists in location"*)
            print_resource_group_conflict_help
            ;;
        "AuthorizationFailed"|*"Unauthorized"*|*"401"*)
            print_auth_error_help
            ;;
        "QuotaExceeded"|*"quota"*)
            print_quota_error_help
            ;;
        "ResourceNotFound"|*"not found"*)
            print_resource_not_found_help
            ;;
        *)
            print_generic_error_help
            ;;
    esac
}

print_location_error_help() {
    echo -e "\n${YELLOW}📍 This Azure region doesn't support the resource type you're deploying.${NC}\n"

    echo -e "${BOLD}Suggested Solutions:${NC}"
    echo -e "  1. ${GREEN}Use a supported region${NC}"
    echo -e "     For Static Web Apps, try: ${CYAN}westus2, centralus, eastus2, westeurope, eastasia${NC}\n"

    echo -e "  2. ${GREEN}Check available regions for the resource type:${NC}"
    echo -e "     ${CYAN}az provider show --namespace Microsoft.Web --query \"resourceTypes[?resourceType=='staticSites'].locations\" -o tsv${NC}\n"

    echo -e "  3. ${GREEN}Quick fix - update location parameter:${NC}"
    echo -e "     ${CYAN}./deploy-dev.sh -l westus2${NC}  # or another supported region\n"

    echo -e "${BLUE}📚 More info: https://azure.microsoft.com/explore/global-infrastructure/products-by-region/${NC}\n"
}

print_resource_group_conflict_help() {
    echo -e "\n${YELLOW}📦 Resource group already exists in a different location.${NC}\n"

    echo -e "${BOLD}Suggested Solutions:${NC}"
    echo -e "  1. ${GREEN}Use the existing resource group's location:${NC}"
    echo -e "     ${CYAN}EXISTING_LOCATION=\$(az group show -n <rg-name> --query location -o tsv)${NC}"
    echo -e "     ${CYAN}./deploy-dev.sh -l \$EXISTING_LOCATION${NC}\n"

    echo -e "  2. ${GREEN}Delete existing resource group (if safe):${NC}"
    echo -e "     ${CYAN}az group delete --name <rg-name> --yes${NC}"
    echo -e "     ${YELLOW}⚠️  Wait 1-2 minutes, then run deployment again${NC}\n"

    echo -e "  3. ${GREEN}Use a different resource group name:${NC}"
    echo -e "     ${CYAN}./deploy-dev.sh -g <new-rg-name> -l <preferred-location>${NC}\n"
}

print_auth_error_help() {
    echo -e "\n${YELLOW}🔑 Authentication or authorization issue detected.${NC}\n"

    echo -e "${BOLD}Suggested Solutions:${NC}"
    echo -e "  1. ${GREEN}Re-authenticate with Azure:${NC}"
    echo -e "     ${CYAN}az login${NC}\n"

    echo -e "  2. ${GREEN}For headless environments (CI/CD, remote):${NC}"
    echo -e "     ${CYAN}az login --use-device-code${NC}\n"

    echo -e "  3. ${GREEN}Verify your subscription:${NC}"
    echo -e "     ${CYAN}az account show${NC}"
    echo -e "     ${CYAN}az account set --subscription <subscription-id>${NC}\n"

    echo -e "  4. ${GREEN}Check your role assignments:${NC}"
    echo -e "     ${CYAN}az role assignment list --assignee \$(az account show --query user.name -o tsv)${NC}\n"
}

print_quota_error_help() {
    echo -e "\n${YELLOW}📊 Resource quota exceeded.${NC}\n"

    echo -e "${BOLD}Suggested Solutions:${NC}"
    echo -e "  1. ${GREEN}Check current resource usage:${NC}"
    echo -e "     ${CYAN}az vm list-usage --location <location> -o table${NC}\n"

    echo -e "  2. ${GREEN}Request a quota increase:${NC}"
    echo -e "     Visit: ${CYAN}https://portal.azure.com/#blade/Microsoft_Azure_Support/HelpAndSupportBlade/newsupportrequest${NC}\n"

    echo -e "  3. ${GREEN}Try a different region:${NC}"
    echo -e "     ${CYAN}./deploy-dev.sh -l <different-region>${NC}\n"

    echo -e "  4. ${GREEN}Clean up unused resources:${NC}"
    echo -e "     ${CYAN}az resource list --resource-group <rg-name> -o table${NC}\n"
}

print_resource_not_found_help() {
    echo -e "\n${YELLOW}🔍 Resource not found.${NC}\n"

    echo -e "${BOLD}Suggested Solutions:${NC}"
    echo -e "  1. ${GREEN}Verify the resource exists:${NC}"
    echo -e "     ${CYAN}az resource list --resource-group <rg-name> -o table${NC}\n"

    echo -e "  2. ${GREEN}Check resource group exists:${NC}"
    echo -e "     ${CYAN}az group show --name <rg-name>${NC}\n"

    echo -e "  3. ${GREEN}Verify you're in the correct subscription:${NC}"
    echo -e "     ${CYAN}az account show${NC}\n"
}

print_generic_error_help() {
    echo -e "\n${YELLOW}💡 General troubleshooting steps:${NC}\n"

    echo -e "${BOLD}Basic Checks:${NC}"
    echo -e "  1. ${GREEN}Verify Azure CLI is up to date:${NC}"
    echo -e "     ${CYAN}az upgrade${NC}\n"

    echo -e "  2. ${GREEN}Check you're logged in:${NC}"
    echo -e "     ${CYAN}az account show${NC}\n"

    echo -e "  3. ${GREEN}Validate Bicep template:${NC}"
    echo -e "     ${CYAN}az bicep build --file main.bicep${NC}\n"

    echo -e "  4. ${GREEN}Check deployment history for details:${NC}"
    echo -e "     ${CYAN}az deployment group list --resource-group <rg-name> -o table${NC}\n"

    echo -e "${BOLD}Get More Help:${NC}"
    echo -e "  • Check Azure Status: ${CYAN}https://status.azure.com${NC}"
    echo -e "  • Review deployment logs in Azure Portal"
    echo -e "  • Run with --debug for verbose output\n"
}

# Check prerequisites and provide helpful output
check_prerequisites() {
    local missing=0

    echo -e "${BOLD}Checking prerequisites...${NC}\n"

    # Check Azure CLI
    if command -v az &> /dev/null; then
        local az_version=$(az version --query '"azure-cli"' -o tsv)
        echo -e "  ${GREEN}✓${NC} Azure CLI v${az_version}"
    else
        echo -e "  ${RED}✗${NC} Azure CLI not found"
        echo -e "    ${CYAN}Install: https://docs.microsoft.com/cli/azure/install-azure-cli${NC}"
        missing=1
    fi

    # Check if logged in
    if az account show &>/dev/null; then
        local account=$(az account show --query name -o tsv)
        echo -e "  ${GREEN}✓${NC} Logged in to: ${account}"
    else
        echo -e "  ${RED}✗${NC} Not logged in to Azure"
        echo -e "    ${CYAN}Run: az login${NC}"
        missing=1
    fi

    # Check Bicep
    if az bicep version &>/dev/null; then
        local bicep_version=$(az bicep version 2>&1 | grep -oP '\d+\.\d+\.\d+' | head -1)
        echo -e "  ${GREEN}✓${NC} Bicep v${bicep_version:-installed}"
    else
        echo -e "  ${YELLOW}!${NC} Bicep not installed (will auto-install on first use)"
    fi

    echo ""
    return $missing
}

# Wrap a command with error handling
run_with_troubleshooting() {
    local description="$1"
    shift
    local command="$@"

    echo -e "${BLUE}▶${NC} ${description}..."

    # Run the command and capture output and exit code
    local output
    local exit_code
    output=$("$@" 2>&1)
    exit_code=$?

    if [ $exit_code -eq 0 ]; then
        echo -e "${GREEN}✓${NC} ${description} completed"
        echo "$output"
        return 0
    else
        # Extract error code from output
        local error_code=""
        if [[ "$output" =~ \"code\":\"([^\"]+)\" ]]; then
            error_code="${BASH_REMATCH[1]}"
        elif [[ "$output" =~ Code:\ *([A-Za-z]+) ]]; then
            error_code="${BASH_REMATCH[1]}"
        fi

        print_error_with_help "$description failed" "${error_code:-$output}"
        echo -e "\n${BOLD}Full error output:${NC}"
        echo "$output"
        return $exit_code
    fi
}

# Show quick navigation menu
show_recovery_menu() {
    echo -e "\n${BOLD}What would you like to do?${NC}"
    echo -e "  ${CYAN}1${NC}) Retry the deployment"
    echo -e "  ${CYAN}2${NC}) Change location and retry"
    echo -e "  ${CYAN}3${NC}) View deployment logs"
    echo -e "  ${CYAN}4${NC}) Check Azure status"
    echo -e "  ${CYAN}5${NC}) Exit"
    echo ""
    read -p "Select option [1-5]: " choice

    case $choice in
        1) return 0 ;;  # Retry
        2)
            read -p "Enter new location: " new_location
            export LOCATION="$new_location"
            return 0
            ;;
        3)
            az deployment group list --resource-group "$RESOURCE_GROUP" -o table 2>/dev/null || echo "No deployments found"
            return 1
            ;;
        4)
            echo "Opening Azure Status page..."
            if command -v xdg-open &> /dev/null; then
                xdg-open "https://status.azure.com" 2>/dev/null &
            else
                echo "Visit: https://status.azure.com"
            fi
            return 1
            ;;
        *) exit 0 ;;
    esac
}

# Export functions
export -f print_header
export -f print_error_with_help
export -f check_prerequisites
export -f run_with_troubleshooting
export -f show_recovery_menu
