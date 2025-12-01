# Git Helper Functions
# Provides functions for git repository operations

function Get-GitRepositoryStatus {
    <#
    .SYNOPSIS
    Gets the current git repository status.
    #>
    param(
        [string]$RepositoryPath = "."
    )
    
    try {
        Push-Location $RepositoryPath
        $status = git status --porcelain 2>$null
        $branch = git rev-parse --abbrev-ref HEAD 2>$null
        $isRepo = Test-Path ".git"
        
        return @{
            HasUncommittedChanges = ($status -ne $null -and $status.Length -gt 0)
            Branch = $branch
            IsRepository = $isRepo
            Status = $status
        }
    }
    catch {
        return @{
            HasUncommittedChanges = $false
            Branch = ""
            IsRepository = $false
            Status = ""
        }
    }
    finally {
        Pop-Location
    }
}

function Commit-GitChanges {
    <#
    .SYNOPSIS
    Commits changes to git repository.
    #>
    param(
        [string]$Message = "Trigger deployment",
        [string]$RepositoryPath = ".",
        [switch]$AllowEmpty
    )
    
    try {
        Push-Location $RepositoryPath
        
        if ($AllowEmpty) {
            git commit --allow-empty -m $Message 2>&1 | Out-Null
        } else {
            git commit -m $Message 2>&1 | Out-Null
        }
        
        return $LASTEXITCODE -eq 0
    }
    catch {
        Write-Log "Failed to commit changes: $($_.Exception.Message)" "ERROR"
        return $false
    }
    finally {
        Pop-Location
    }
}

function Push-GitBranch {
    <#
    .SYNOPSIS
    Pushes a branch to remote.
    #>
    param(
        [string]$Branch,
        [string]$Remote = "origin",
        [string]$RepositoryPath = "."
    )
    
    try {
        Push-Location $RepositoryPath
        git push $Remote $Branch 2>&1 | Out-Null
        return $LASTEXITCODE -eq 0
    }
    catch {
        Write-Log "Failed to push branch: $($_.Exception.Message)" "ERROR"
        return $false
    }
    finally {
        Pop-Location
    }
}

function Get-GitRemoteUrl {
    <#
    .SYNOPSIS
    Gets the remote URL for a git repository.
    #>
    param(
        [string]$Remote = "origin",
        [string]$RepositoryPath = "."
    )
    
    try {
        Push-Location $RepositoryPath
        $url = git remote get-url $Remote 2>$null
        return $url
    }
    catch {
        return ""
    }
    finally {
        Pop-Location
    }
}

function Sync-GitRepository {
    <#
    .SYNOPSIS
    Fetches latest changes and ensures branch is up to date.
    #>
    param(
        [string]$Branch,
        [string]$RepositoryPath = "."
    )
    
    try {
        Push-Location $RepositoryPath
        
        # Fetch latest
        git fetch origin 2>&1 | Out-Null
        
        # Check if branch exists on remote
        $remoteBranch = "origin/$Branch"
        $branchExists = git branch -r --list $remoteBranch 2>$null
        
        if ($branchExists) {
            # Pull latest changes
            git pull origin $Branch 2>&1 | Out-Null
        }
        
        return $LASTEXITCODE -eq 0
    }
    catch {
        Write-Log "Failed to sync repository: $($_.Exception.Message)" "ERROR"
        return $false
    }
    finally {
        Pop-Location
    }
}

Export-ModuleMember -Function Get-GitRepositoryStatus, Commit-GitChanges, Push-GitBranch, Get-GitRemoteUrl, Sync-GitRepository

