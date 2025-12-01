# PowerShell script to download video from SharePoint
# Usage: .\scripts\download-sharepoint-video.ps1

param(
    [string]$SharePointUrl = "https://phoenixvc568-my.sharepoint.com/:v:/g/personal/eben_phoenixvc_tech/IQDpDRXOYaqYRJcfbDXMwngeAQT5Z7YE9lacgDAoUUjzI80?e=CM77KG",
    [string]$OutputPath = "src/Mystira.App.PWA/wwwroot/videos/logo-intro.mp4"
)

Write-Host "Downloading video from SharePoint..." -ForegroundColor Cyan
Write-Host "URL: $SharePointUrl" -ForegroundColor Gray
Write-Host "Output: $OutputPath" -ForegroundColor Gray

# Create output directory if it doesn't exist
$outputDir = Split-Path -Parent $OutputPath
if (-not (Test-Path $outputDir)) {
    New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
    Write-Host "Created directory: $outputDir" -ForegroundColor Green
}

# Note: SharePoint video links require authentication
# You'll need to either:
# 1. Download manually from the SharePoint link in your browser
# 2. Use Microsoft Graph API with authentication
# 3. Use SharePoint REST API with authentication

Write-Host ""
Write-Host "⚠️  Manual Download Required" -ForegroundColor Yellow
Write-Host "SharePoint videos require authentication. Please:" -ForegroundColor Yellow
Write-Host "1. Open the SharePoint link in your browser" -ForegroundColor White
Write-Host "2. Click the three dots (...) menu on the video player" -ForegroundColor White
Write-Host "3. Select 'Download' or 'Save video as...'" -ForegroundColor White
Write-Host "4. Save the file to: $OutputPath" -ForegroundColor White
Write-Host ""
Write-Host "Alternatively, you can use Microsoft Graph PowerShell:" -ForegroundColor Cyan
Write-Host "  Install-Module Microsoft.Graph -Scope CurrentUser" -ForegroundColor Gray
Write-Host "  Connect-MgGraph -Scopes Files.Read.All" -ForegroundColor Gray
Write-Host "  # Then use Graph API to download the file" -ForegroundColor Gray
Write-Host ""

# Check if file already exists
if (Test-Path $OutputPath) {
    Write-Host "✅ File already exists at: $OutputPath" -ForegroundColor Green
    $fileInfo = Get-Item $OutputPath
    Write-Host "   Size: $([math]::Round($fileInfo.Length / 1MB, 2)) MB" -ForegroundColor Gray
    Write-Host "   Modified: $($fileInfo.LastWriteTime)" -ForegroundColor Gray
} else {
    Write-Host "❌ File not found. Please download manually." -ForegroundColor Red
}

