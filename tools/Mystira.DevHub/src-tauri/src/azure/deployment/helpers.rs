//! Shared helper functions for Azure deployment operations.
//!
//! This module contains utility functions used across deployment commands:
//! - Resource group name resolution
//! - Deployment path construction
//! - Azure CLI validation and authentication
//! - Parameter file generation
//!
//! These functions help reduce code duplication across deploy, validate, preview, and status commands.

use crate::helpers::{check_azure_cli_installed, check_winget_available, get_azure_subscription_id, get_azure_cli_path};
use crate::types::CommandResponse;
use std::process::Command;

/// Get resource group name from environment
pub fn get_resource_group_name(environment: &str) -> String {
    match environment {
        "dev" => "dev-san-rg-mystira-app".to_string(),
        "prod" => "prod-san-rg-mystira-app".to_string(),
        _ => format!("{}-san-rg-mystira-app", environment),
    }
}

/// Get deployment path from repo root and environment
pub fn get_deployment_path(repo_root: &str, environment: &str) -> String {
    format!(
        "{}/src/Mystira.App.Infrastructure.Azure/Deployment/{}",
        repo_root, environment
    )
}

/// Check Azure CLI installation and return error response if not installed
pub fn check_azure_cli_or_error() -> Option<CommandResponse> {
    if !check_azure_cli_installed() {
        let winget_available = check_winget_available();
        let error_msg = if winget_available {
            "Azure CLI is not installed. You can install it automatically using winget.".to_string()
        } else {
            "Azure CLI is not installed. Please install it manually from https://aka.ms/installazurecliwindows".to_string()
        };
        return Some(CommandResponse {
            success: false,
            result: Some(serde_json::json!({
                "azureCliMissing": true,
                "wingetAvailable": winget_available,
            })),
            message: None,
            error: Some(error_msg),
        });
    }
    None
}

/// Set Azure subscription
pub fn set_azure_subscription(sub_id: &str) -> Result<(), String> {
    let (az_path, use_direct_path) = get_azure_cli_path();
    
    let result = if use_direct_path {
        Command::new("powershell")
            .arg("-NoProfile")
            .arg("-Command")
            .arg(format!("& '{}' account set --subscription '{}'", az_path.replace("'", "''"), sub_id.replace("'", "''")))
            .output()
    } else {
        Command::new("az")
            .arg("account")
            .arg("set")
            .arg("--subscription")
            .arg(sub_id)
            .output()
    };

    match result {
        Ok(output) => {
            if output.status.success() {
                Ok(())
            } else {
                Err(format!("Failed to set subscription: {}", sub_id))
            }
        }
        Err(e) => Err(format!("Failed to set subscription: {}", e)),
    }
}

/// Check if resource group exists
pub fn check_resource_group_exists(resource_group: &str) -> Result<bool, String> {
    let (az_path, use_direct_path) = get_azure_cli_path();
    
    let result = if use_direct_path {
        Command::new("powershell")
            .arg("-NoProfile")
            .arg("-Command")
            .arg(format!("& '{}' group exists --name '{}' --output 'tsv'", az_path.replace("'", "''"), resource_group.replace("'", "''")))
            .output()
    } else {
        Command::new("az")
            .arg("group")
            .arg("exists")
            .arg("--name")
            .arg(resource_group)
            .arg("--output")
            .arg("tsv")
            .output()
    };

    match result {
        Ok(output) => {
            let stdout = String::from_utf8_lossy(&output.stdout);
            Ok(stdout.trim().to_lowercase() == "true")
        }
        Err(e) => Err(format!("Failed to check resource group: {}", e))
    }
}

/// Create resource group if it doesn't exist
pub fn ensure_resource_group(resource_group: &str, location: &str) -> Result<(), String> {
    let (az_path, use_direct_path) = get_azure_cli_path();
    
    let _result = if use_direct_path {
        Command::new("powershell")
            .arg("-NoProfile")
            .arg("-Command")
            .arg(format!("& '{}' group create --name '{}' --location '{}' --output 'none'", az_path.replace("'", "''"), resource_group.replace("'", "''"), location.replace("'", "''")))
            .output()
    } else {
        Command::new("az")
            .arg("group")
            .arg("create")
            .arg("--name")
            .arg(resource_group)
            .arg("--location")
            .arg(location)
            .arg("--output")
            .arg("none")
            .output()
    };

    // Ignore errors if resource group already exists
    Ok(())
}

/// Build parameters JSON string for deployment
pub fn build_parameters_json(
    environment: &str,
    location: &str,
    deploy_storage: bool,
    deploy_cosmos: bool,
    deploy_app_service: bool,
) -> String {
    format!(
        r#"{{"environment":{{"value":"{}"}},"location":{{"value":"{}"}},"deployStorage":{{"value":{}}},"deployCosmos":{{"value":{}}},"deployAppService":{{"value":{}}}}}"#,
        environment, location, deploy_storage, deploy_cosmos, deploy_app_service
    )
}

/// Check if logged into Azure
pub fn check_azure_login() -> Result<(), CommandResponse> {
    let (az_path, use_direct_path) = get_azure_cli_path();
    
    let account_check = if use_direct_path {
        Command::new("powershell")
            .arg("-NoProfile")
            .arg("-Command")
            .arg(format!("& '{}' account show", az_path.replace("'", "''")))
            .output()
    } else {
        Command::new("az")
            .arg("account")
            .arg("show")
            .output()
    };
    
    match account_check {
        Ok(output) => {
            if output.status.success() {
                Ok(())
            } else {
                Err(CommandResponse {
                    success: false,
                    result: None,
                    message: None,
                    error: Some("Not logged in to Azure. Please run 'az login' first.".to_string()),
                })
            }
        }
        Err(_) => Err(CommandResponse {
            success: false,
            result: None,
            message: None,
            error: Some("Not logged in to Azure. Please run 'az login' first.".to_string()),
        }),
    }
}

/// Get subscription ID with fallback
pub fn get_subscription_id() -> String {
    get_azure_subscription_id().unwrap_or_else(|_| "22f9eb18-6553-4b7d-9451-47d0195085fe".to_string())
}

