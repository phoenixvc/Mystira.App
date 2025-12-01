# Helper script to get Static Web App deployment token
# Usage: .\GetSwaDeploymentToken.ps1 -Name <swa-name> -ResourceGroup <rg-name>

param(
    [Parameter(Mandatory = $true)]
    [string]$Name,
    
    [Parameter(Mandatory = $true)]
    [string]$ResourceGroup
)

Write-Output "Getting deployment token for Static Web App: $Name"
Write-Output "Resource Group: $ResourceGroup"
Write-Output ""

$token = az staticwebapp secrets list --name $Name --resource-group $ResourceGroup --query "properties.apiKey" -o tsv 2>$null

if ($token) {
    Write-Output "Deployment Token:"
    Write-Output $token
    Write-Output ""
    Write-Output "Add this to GitHub Secrets as:"
    Write-Output "  AZURE_STATIC_WEB_APPS_API_TOKEN_DEV_MYSTIRA_APP"
    Write-Output ""
    Write-Output "Or if using the auto-generated workflow, check the workflow file for the expected secret name."
}
else {
    Write-Output "Failed to retrieve deployment token. Make sure:"
    Write-Output "  1. You're logged in to Azure (az login)"
    Write-Output "  2. The Static Web App exists: $Name"
    Write-Output "  3. You have permissions to read secrets"
}

