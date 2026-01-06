use crate::helpers::{check_azure_cli_installed, check_winget_available};
use crate::types::CommandResponse;
use std::process::Command;

/// Check if Azure CLI is installed
#[tauri::command]
pub async fn check_azure_cli() -> Result<CommandResponse, String> {
    let is_installed = check_azure_cli_installed();
    let winget_available = check_winget_available();
    
    Ok(CommandResponse {
        success: true,
        result: Some(serde_json::json!({
            "installed": is_installed,
            "wingetAvailable": winget_available,
        })),
        message: if is_installed {
            Some("Azure CLI is installed".to_string())
        } else {
            Some("Azure CLI is not installed".to_string())
        },
        error: None,
    })
}

/// Install Azure CLI using winget
#[tauri::command]
pub async fn install_azure_cli() -> Result<CommandResponse, String> {
    #[cfg(target_os = "windows")]
    {
        // Check if winget is available
        if !check_winget_available() {
            return Ok(CommandResponse {
                success: false,
                result: None,
                message: None,
                error: Some("winget is not available. Please install Azure CLI manually from https://aka.ms/installazurecliwindows".to_string()),
            });
        }
        
        // Install Azure CLI via winget in a visible terminal window
        // Use cmd /c start to open a new visible PowerShell window that stays open
        let spawn_result = Command::new("cmd")
            .arg("/c")
            .arg("start")
            .arg("PowerShell")
            .arg("-NoExit")
            .arg("-Command")
            .arg("winget install Microsoft.AzureCLI --accept-package-agreements --accept-source-agreements; Write-Host 'Installation complete. You can close this window.'; pause")
            .spawn();
        
        match spawn_result {
            Ok(_) => {
                Ok(CommandResponse {
                    success: true,
                    result: Some(serde_json::json!({
                        "message": "A terminal window has opened to install Azure CLI. After installation completes, please RESTART the application for Azure CLI to be detected.",
                        "requiresRestart": true
                    })),
                    message: Some("Azure CLI installation window opened. Please restart the app after installation.".to_string()),
                    error: None,
                })
            }
            Err(e) => Ok(CommandResponse {
                success: false,
                result: None,
                message: None,
                error: Some(format!("Failed to open installation window: {}. Please install Azure CLI manually from https://aka.ms/installazurecliwindows", e)),
            }),
        }
    }
    
    #[cfg(not(target_os = "windows"))]
    {
        Ok(CommandResponse {
            success: false,
            result: None,
            message: None,
            error: Some("Automatic installation is only available on Windows. Please install Azure CLI manually: https://docs.microsoft.com/cli/azure/install-azure-cli".to_string()),
        })
    }
}

