# Start the Main API
Write-Host "Starting Mystira.App API..." -ForegroundColor Green
Write-Host "API will be available at:" -ForegroundColor Cyan
Write-Host "  - HTTPS: https://localhost:7096" -ForegroundColor Yellow
Write-Host "  - HTTP:  http://localhost:5260" -ForegroundColor Yellow
Write-Host "  - Swagger: https://localhost:7096/swagger" -ForegroundColor Yellow
Write-Host ""

$repoRoot = Split-Path -Parent $PSScriptRoot
Set-Location "$repoRoot\src\Mystira.App.Api"
dotnet run

