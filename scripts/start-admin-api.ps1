# Start the Admin API
Write-Host "Starting Mystira.App Admin API..." -ForegroundColor Green
Write-Host "Admin API will be available at:" -ForegroundColor Cyan
Write-Host "  - HTTPS: https://localhost:7096" -ForegroundColor Yellow
Write-Host "  - HTTP:  http://localhost:5260" -ForegroundColor Yellow
Write-Host "  - Admin UI: https://localhost:7096/admin" -ForegroundColor Yellow
Write-Host "  - Swagger: https://localhost:7096/swagger" -ForegroundColor Yellow
Write-Host ""
Write-Host "Note: Admin API uses same ports as main API. Run only one at a time." -ForegroundColor Gray
Write-Host ""

$repoRoot = Split-Path -Parent $PSScriptRoot
Set-Location "$repoRoot\src\Mystira.App.Admin.Api"
dotnet run

