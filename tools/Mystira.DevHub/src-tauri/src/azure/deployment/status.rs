// Azure infrastructure status checking commands

use crate::azure::deployment::helpers::get_resource_group_name;
use crate::helpers::{check_azure_cli_installed, get_azure_cli_path, get_azure_subscription_id};
use crate::types::CommandResponse;
use serde_json::Value;
use std::process::Command;

/// Check if infrastructure resources exist in a resource group
#[tauri::command]
pub async fn check_infrastructure_exists(
    environment: String,
    resource_group: Option<String>,
) -> Result<CommandResponse, String> {
    let rg = resource_group.unwrap_or_else(|| get_resource_group_name(&environment));
    
    let check_rg = Command::new("az")
        .arg("group")
        .arg("exists")
        .arg("--name")
        .arg(&rg)
        .output();
    
    match check_rg {
        Ok(output) => {
            let stdout = String::from_utf8_lossy(&output.stdout);
            let exists = stdout.trim().to_lowercase() == "true";
            
            if !exists {
                return Ok(CommandResponse {
                    success: true,
                    result: Some(serde_json::json!({
                        "exists": false,
                        "resourceGroup": rg,
                        "message": "Resource group does not exist"
                    })),
                    message: Some("Infrastructure not found".to_string()),
                    error: None,
                });
            }
            
            let check_resources = Command::new("az")
                .arg("resource")
                .arg("list")
                .arg("--resource-group")
                .arg(&rg)
                .arg("--output")
                .arg("json")
                .output();
            
            match check_resources {
                Ok(output) => {
                    let stdout = String::from_utf8_lossy(&output.stdout);
                    let resources: Result<Vec<Value>, _> = serde_json::from_str(&stdout);
                    
                    if let Ok(resources) = resources {
                        let has_app_service = resources.iter().any(|r| {
                            let resource_type = r.get("type").and_then(|t| t.as_str()).unwrap_or("");
                            let provisioning_state = r.get("properties")
                                .and_then(|p| p.get("provisioningState"))
                                .and_then(|v| v.as_str())
                                .unwrap_or("");
                            resource_type.contains("Microsoft.Web/sites") && provisioning_state == "Succeeded"
                        });
                        let has_cosmos = resources.iter().any(|r| {
                            let resource_type = r.get("type").and_then(|t| t.as_str()).unwrap_or("");
                            let provisioning_state = r.get("properties")
                                .and_then(|p| p.get("provisioningState"))
                                .and_then(|v| v.as_str())
                                .unwrap_or("");
                            resource_type.contains("Microsoft.DocumentDB") && provisioning_state == "Succeeded"
                        });
                        let has_storage = resources.iter().any(|r| {
                            let resource_type = r.get("type").and_then(|t| t.as_str()).unwrap_or("");
                            let provisioning_state = r.get("properties")
                                .and_then(|p| p.get("provisioningState"))
                                .and_then(|v| v.as_str())
                                .unwrap_or("");
                            resource_type.contains("Microsoft.Storage") && provisioning_state == "Succeeded"
                        });
                        
                        let exists = has_app_service || has_cosmos || has_storage;
                        
                        Ok(CommandResponse {
                            success: true,
                            result: Some(serde_json::json!({
                                "exists": exists,
                                "resourceGroup": rg,
                                "hasAppService": has_app_service,
                                "hasCosmos": has_cosmos,
                                "hasStorage": has_storage,
                                "resourceCount": resources.len()
                            })),
                            message: if exists {
                                Some("Infrastructure exists".to_string())
                            } else {
                                Some("Resource group exists but no infrastructure resources found".to_string())
                            },
                            error: None,
                        })
                    } else {
                        Ok(CommandResponse {
                            success: true,
                            result: Some(serde_json::json!({
                                "exists": false,
                                "resourceGroup": rg,
                                "message": "Could not parse resource list"
                            })),
                            message: Some("Infrastructure status unknown".to_string()),
                            error: None,
                        })
                    }
                }
                Err(e) => Ok(CommandResponse {
                    success: false,
                    result: None,
                    message: None,
                    error: Some(format!("Failed to check resources: {}", e)),
                }),
            }
        }
        Err(e) => Ok(CommandResponse {
            success: false,
            result: None,
            message: None,
            error: Some(format!("Failed to check resource group: {}", e)),
        }),
    }
}

/// Check infrastructure status for a resource group
#[tauri::command]
pub async fn check_infrastructure_status(
    _environment: String,
    resource_group: String,
) -> Result<CommandResponse, String> {
    if !check_azure_cli_installed() {
        return Ok(CommandResponse {
            success: false,
            result: None,
            message: None,
            error: Some("Azure CLI is not installed".to_string()),
        });
    }

    let (az_path, use_direct_path) = get_azure_cli_path();

    let sub_id = get_azure_subscription_id().unwrap_or_else(|_| "22f9eb18-6553-4b7d-9451-47d0195085fe".to_string());
    let _ = if use_direct_path {
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
            .arg(&sub_id)
            .output()
    };

    let output = if use_direct_path {
        Command::new("powershell")
            .arg("-NoProfile")
            .arg("-Command")
            .arg(format!("& '{}' resource list --resource-group '{}' --output json", az_path.replace("'", "''"), resource_group.replace("'", "''")))
            .output()
    } else {
        Command::new("az")
            .arg("resource")
            .arg("list")
            .arg("--resource-group")
            .arg(&resource_group)
            .arg("--output")
            .arg("json")
            .output()
    };

    match output {
        Ok(result) => {
            if result.status.success() {
                let stdout = String::from_utf8_lossy(&result.stdout);
                let resources: Result<Vec<serde_json::Value>, _> = serde_json::from_str(&stdout);
                
                match resources {
                    Ok(resources_vec) => {
                        let mut status = serde_json::json!({
                            "available": false,
                            "resources": {
                                "storage": { "exists": false, "health": "unknown", "instances": [] },
                                "cosmos": { "exists": false, "health": "unknown", "instances": [] },
                                "appService": { "exists": false, "health": "unknown", "instances": [] },
                                "keyVault": { "exists": false, "health": "unknown", "instances": [] }
                            },
                            "lastChecked": std::time::SystemTime::now().duration_since(std::time::UNIX_EPOCH).unwrap().as_secs() * 1000,
                            "resourceGroup": resource_group
                        });

                        let mut storage_instances: Vec<serde_json::Value> = Vec::new();
                        let mut cosmos_instances: Vec<serde_json::Value> = Vec::new();
                        let mut appservice_instances: Vec<serde_json::Value> = Vec::new();
                        let mut keyvault_instances: Vec<serde_json::Value> = Vec::new();

                        for resource in &resources_vec {
                            let resource_type = resource.get("type").and_then(|v| v.as_str()).unwrap_or("");
                            let resource_name = resource.get("name").and_then(|v| v.as_str()).unwrap_or("");
                            let resource_location = resource.get("location").and_then(|v| v.as_str()).unwrap_or("");
                            let provisioning_state = resource.get("properties")
                                .and_then(|p| p.get("provisioningState"))
                                .and_then(|v| v.as_str())
                                .unwrap_or("");
                            
                            let mut runtime_status = "unknown".to_string();
                            let mut runtime_health = "unknown".to_string();
                            
                            if resource_type == "Microsoft.Web/sites" {
                                if let Some(properties) = resource.get("properties") {
                                    if let Some(state) = properties.get("state") {
                                        runtime_status = state.as_str().unwrap_or("unknown").to_string();
                                    }
                                }
                                runtime_health = match runtime_status.as_str() {
                                    "Running" => "healthy",
                                    "Stopped" => "unhealthy",
                                    "Starting" | "Stopping" => "degraded",
                                    _ => "unknown"
                                }.to_string();
                            }
                            
                            let health = if resource_type == "Microsoft.Web/sites" && runtime_health != "unknown" {
                                runtime_health.as_str()
                            } else if provisioning_state == "Succeeded" {
                                "healthy"
                            } else if provisioning_state == "Failed" || provisioning_state == "Canceled" {
                                "unhealthy"
                            } else if provisioning_state == "Updating" || provisioning_state == "Creating" {
                                "degraded"
                            } else {
                                "unknown"
                            };
                            
                            let instance = serde_json::json!({
                                "name": resource_name,
                                "health": health,
                                "location": resource_location,
                                "status": if resource_type == "Microsoft.Web/sites" { runtime_status } else { provisioning_state.to_string() }
                            });
                            
                            let is_provisioned = provisioning_state == "Succeeded";
                            
                            if resource_type == "Microsoft.Storage/storageAccounts" && is_provisioned {
                                storage_instances.push(instance);
                                status["resources"]["storage"]["exists"] = serde_json::json!(true);
                                if storage_instances.len() == 1 {
                                    status["resources"]["storage"]["name"] = serde_json::json!(resource_name);
                                    status["resources"]["storage"]["health"] = serde_json::json!(health);
                                }
                            } else if resource_type == "Microsoft.DocumentDB/databaseAccounts" && is_provisioned {
                                cosmos_instances.push(instance);
                                status["resources"]["cosmos"]["exists"] = serde_json::json!(true);
                                if cosmos_instances.len() == 1 {
                                    status["resources"]["cosmos"]["name"] = serde_json::json!(resource_name);
                                    status["resources"]["cosmos"]["health"] = serde_json::json!(health);
                                }
                            } else if resource_type == "Microsoft.Web/sites" && is_provisioned {
                                appservice_instances.push(instance);
                                status["resources"]["appService"]["exists"] = serde_json::json!(true);
                                if appservice_instances.len() == 1 {
                                    status["resources"]["appService"]["name"] = serde_json::json!(resource_name);
                                    status["resources"]["appService"]["health"] = serde_json::json!(health);
                                }
                            } else if resource_type == "Microsoft.KeyVault/vaults" && is_provisioned {
                                keyvault_instances.push(instance);
                                status["resources"]["keyVault"]["exists"] = serde_json::json!(true);
                                if keyvault_instances.len() == 1 {
                                    status["resources"]["keyVault"]["name"] = serde_json::json!(resource_name);
                                    status["resources"]["keyVault"]["health"] = serde_json::json!(health);
                                }
                            }
                        }
                        
                        status["resources"]["storage"]["instances"] = serde_json::json!(storage_instances);
                        status["resources"]["cosmos"]["instances"] = serde_json::json!(cosmos_instances);
                        status["resources"]["appService"]["instances"] = serde_json::json!(appservice_instances);
                        status["resources"]["keyVault"]["instances"] = serde_json::json!(keyvault_instances);

                        let has_storage = status["resources"]["storage"]["exists"].as_bool().unwrap_or(false);
                        let has_cosmos = status["resources"]["cosmos"]["exists"].as_bool().unwrap_or(false);
                        let has_app_service = status["resources"]["appService"]["exists"].as_bool().unwrap_or(false);
                        status["available"] = serde_json::json!(has_storage || has_cosmos || has_app_service);

                        Ok(CommandResponse {
                            success: true,
                            result: Some(status),
                            message: None,
                            error: None,
                        })
                    }
                    Err(e) => Ok(CommandResponse {
                        success: false,
                        result: None,
                        message: None,
                        error: Some(format!("Failed to parse resources: {}", e)),
                    }),
                }
            } else {
                let status = serde_json::json!({
                    "available": false,
                    "resources": {
                        "storage": { "exists": false, "health": "unknown" },
                        "cosmos": { "exists": false, "health": "unknown" },
                        "appService": { "exists": false, "health": "unknown" },
                        "keyVault": { "exists": false, "health": "unknown" }
                    },
                    "lastChecked": std::time::SystemTime::now().duration_since(std::time::UNIX_EPOCH).unwrap().as_secs() * 1000,
                    "resourceGroup": resource_group
                });

                Ok(CommandResponse {
                    success: true,
                    result: Some(status),
                    message: None,
                    error: None,
                })
            }
        }
        Err(e) => Ok(CommandResponse {
            success: false,
            result: None,
            message: None,
            error: Some(format!("Failed to check infrastructure: {}", e)),
        }),
    }
}

