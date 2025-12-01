# Retry Helper Functions
# Provides retry logic with exponential backoff for transient failures

function Test-TransientError {
    <#
    .SYNOPSIS
    Determines if an error is transient and should be retried.
    
    .PARAMETER ErrorMessage
    The error message to check
    
    .PARAMETER ErrorCode
    Optional error code
    #>
    param(
        [string]$ErrorMessage,
        [string]$ErrorCode = ""
    )
    
    $transientPatterns = @(
        "ServiceUnavailable",
        "TooManyRequests",
        "RequestTimeout",
        "GatewayTimeout",
        "InternalServerError",
        "BadGateway",
        "Service temporarily unavailable",
        "rate limit",
        "throttle",
        "timeout",
        "network",
        "connection",
        "temporarily unavailable"
    )
    
    $errorText = "$ErrorMessage $ErrorCode".ToLower()
    
    foreach ($pattern in $transientPatterns) {
        if ($errorText -match $pattern.ToLower()) {
            return $true
        }
    }
    
    return $false
}

function Invoke-WithRetry {
    <#
    .SYNOPSIS
    Executes a command with retry logic for transient failures.
    
    .PARAMETER ScriptBlock
    The script block to execute
    
    .PARAMETER MaxRetries
    Maximum number of retry attempts
    
    .PARAMETER InitialDelaySeconds
    Initial delay before first retry (in seconds)
    
    .PARAMETER MaxDelaySeconds
    Maximum delay between retries (in seconds)
    
    .PARAMETER BackoffMultiplier
    Multiplier for exponential backoff
    
    .PARAMETER ErrorMessage
    Error message to check for transient errors
    
    .PARAMETER ErrorCode
    Error code to check for transient errors
    #>
    param(
        [scriptblock]$ScriptBlock,
        [int]$MaxRetries = 3,
        [int]$InitialDelaySeconds = 2,
        [int]$MaxDelaySeconds = 60,
        [double]$BackoffMultiplier = 2.0,
        [string]$ErrorMessage = "",
        [string]$ErrorCode = ""
    )
    
    $attempt = 0
    $delay = $InitialDelaySeconds
    
    while ($attempt -le $MaxRetries) {
        try {
            $result = & $ScriptBlock
            return @{
                Success = $true
                Result = $result
                Attempts = $attempt + 1
            }
        } catch {
            $attempt++
            $lastError = $_.Exception.Message
            
            # Check if error is transient
            if (-not (Test-TransientError -ErrorMessage $lastError -ErrorCode $ErrorCode)) {
                Write-Log "Non-transient error detected, not retrying: $lastError" "ERROR"
                return @{
                    Success = $false
                    Error = $lastError
                    Attempts = $attempt
                    IsTransient = $false
                }
            }
            
            # Check if we've exceeded max retries
            if ($attempt -gt $MaxRetries) {
                Write-Log "Max retries ($MaxRetries) exceeded for transient error: $lastError" "ERROR"
                return @{
                    Success = $false
                    Error = $lastError
                    Attempts = $attempt
                    IsTransient = $true
                }
            }
            
            # Calculate delay with exponential backoff
            $actualDelay = [Math]::Min($delay, $MaxDelaySeconds)
            
            Write-ColorOutput Yellow "⚠️  Transient error detected (attempt $attempt/$MaxRetries): $lastError"
            Write-ColorOutput Cyan "   Retrying in $actualDelay seconds..."
            Write-Log "Transient error on attempt $attempt, retrying in $actualDelay seconds: $lastError" "WARN"
            
            Start-Sleep -Seconds $actualDelay
            
            # Increase delay for next retry
            $delay = [Math]::Min($delay * $BackoffMultiplier, $MaxDelaySeconds)
        }
    }
    
    return @{
        Success = $false
        Error = "Max retries exceeded"
        Attempts = $attempt
        IsTransient = $true
    }
}

function Invoke-AzureCliWithRetry {
    <#
    .SYNOPSIS
    Executes an Azure CLI command with retry logic for transient failures.
    
    .PARAMETER Command
    The Azure CLI command to execute (as a string)
    
    .PARAMETER MaxRetries
    Maximum number of retry attempts
    
    .PARAMETER TimeoutSeconds
    Timeout for the command
    #>
    param(
        [string]$Command,
        [int]$MaxRetries = 3,
        [int]$TimeoutSeconds = 30
    )
    
    $result = Invoke-WithRetry -ScriptBlock {
        # Use Invoke-AzCliWithTimeout if available, otherwise use Invoke-Expression
        if (Get-Command Invoke-AzCliWithTimeout -ErrorAction SilentlyContinue) {
            Invoke-AzCliWithTimeout -Command $Command -TimeoutSeconds $TimeoutSeconds
        } else {
            Invoke-Expression $Command
        }
    } -MaxRetries $MaxRetries
    
    return $result
}

Export-ModuleMember -Function Test-TransientError, Invoke-WithRetry, Invoke-AzureCliWithRetry

