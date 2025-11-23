# Start the Admin API
Write-Host "Starting Mystira.App Admin API..." -ForegroundColor Green
Write-Host "Admin API will be available at:" -ForegroundColor Cyan
Write-Host "  - HTTPS: https://localhost:7096" -ForegroundColor Yellow
Write-Host "  - HTTP:  http://localhost:5260" -ForegroundColor Yellow
Write-Host "  - Admin UI: https://localhost:7096/admin" -ForegroundColor Yellow
Write-Host ""

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
Set-Location "$repoRoot\src\Mystira.App.Admin.Api"
dotnet run

