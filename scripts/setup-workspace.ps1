# setup-workspace.ps1 - Clone all Mystira repositories as siblings
# Run this script from within Mystira.App directory

$ErrorActionPreference = "Stop"

$ParentDir = Split-Path -Parent (Get-Location)
$GitHubOrg = "phoenixvc"

# All Mystira repositories
$repos = @(
    "Mystira.Chain",
    "Mystira.Infra",
    "Mystira.StoryGenerator",
    "Mystira.workspace"
)

Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "Mystira Workspace Setup" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "Parent directory: $ParentDir"
Write-Host ""

# Check if we're in Mystira.App
if (-not (Test-Path "Mystira.App.sln")) {
    Write-Host "Warning: Run this script from within the Mystira.App directory" -ForegroundColor Yellow
    Write-Host "Current directory: $(Get-Location)"
}

Write-Host "Cloning sibling repositories..." -ForegroundColor Cyan
Write-Host ""

foreach ($repo in $repos) {
    $repoPath = Join-Path $ParentDir $repo

    if (Test-Path $repoPath) {
        Write-Host "[OK] $repo already exists" -ForegroundColor Green
    } else {
        Write-Host "[CLONE] Cloning $repo..." -ForegroundColor Yellow
        try {
            git clone "https://github.com/$GitHubOrg/$repo.git" $repoPath 2>$null
            Write-Host "  [OK] Cloned successfully" -ForegroundColor Green
        } catch {
            Write-Host "  [SKIP] Repository not accessible or doesn't exist yet" -ForegroundColor Gray
        }
    }
}

Write-Host ""
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "Setup complete!" -ForegroundColor Green
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Directory structure:"
Get-ChildItem $ParentDir | Format-Table Name, LastWriteTime
Write-Host ""
Write-Host "To open workspace in VS Code:" -ForegroundColor Cyan
Write-Host "  code $ParentDir\Mystira.workspace\mystira.code-workspace" -ForegroundColor Gray
Write-Host ""
Write-Host "Or if workspace not yet created, open individual repos:" -ForegroundColor Cyan
Write-Host "  code $ParentDir\Mystira.App" -ForegroundColor Gray
