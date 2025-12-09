# Error Formatter Module
# Standardizes error message formatting

function Format-Error {
    <#
    .SYNOPSIS
    Formats error messages consistently.
    
    .PARAMETER ErrorMessage
    The raw error message
    
    .PARAMETER Step
    The step where the error occurred
    
    .PARAMETER ErrorCode
    Optional error code
    #>
    param(
        [string]$ErrorMessage,
        [string]$Step,
        [string]$ErrorCode = ""
    )
    
    $formatted = @()
    
    if ($Step) {
        $formatted += "❌ Error at step: $Step"
    }
    else {
        $formatted += "❌ Error occurred"
    }
    
    if ($ErrorCode) {
        $formatted += "   Error Code: $ErrorCode"
    }
    
    # Try to extract user-friendly message from JSON errors
    if ($ErrorMessage -match '{"error"') {
        try {
            $errorObj = $ErrorMessage | ConvertFrom-Json -ErrorAction SilentlyContinue
            if ($errorObj.error) {
                $formatted += "   Message: $($errorObj.error.message)"
                if ($errorObj.error.details) {
                    foreach ($detail in $errorObj.error.details) {
                        if ($detail.message) {
                            $formatted += "   Details: $($detail.message)"
                        }
                    }
                }
            }
            else {
                $formatted += "   Message: $ErrorMessage"
            }
        }
        catch {
            $formatted += "   Message: $ErrorMessage"
        }
    }
    else {
        $formatted += "   Message: $ErrorMessage"
    }
    
    return $formatted -join "`n"
}

function Format-AzureError {
    <#
    .SYNOPSIS
    Formats Azure-specific error messages.
    #>
    param(
        [string]$ErrorJson,
        [string]$Step
    )
    
    try {
        $errorObj = $ErrorJson | ConvertFrom-Json -ErrorAction Stop
        $message = ""
        
        if ($errorObj.error) {
            $message = $errorObj.error.message
            if ($errorObj.error.code) {
                $code = $errorObj.error.code
                return Format-Error -ErrorMessage $message -Step $Step -ErrorCode $code
            }
        }
        
        return Format-Error -ErrorMessage $message -Step $Step
    }
    catch {
        return Format-Error -ErrorMessage $ErrorJson -Step $Step
    }
}

Export-ModuleMember -Function Format-Error, Format-AzureError

