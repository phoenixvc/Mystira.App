// Azure resource management commands

use crate::helpers::{check_azure_cli_installed, check_winget_available, get_azure_subscription_id, get_azure_cli_path};
use crate::types::CommandResponse;
use crate::cache::{AZURE_RESOURCES_CACHE, get_cache_ttl};
use crate::rate_limit::wait_azure_rate_limit;
use std::process::Command;
use tracing::{info, warn, error, debug};

/// Get Azure resources, optionally filtered by environment
#[tauri::command]
pub async fn get_azure_resources(subscription_id: Option<String>, environment: Option<String>) -> Result<CommandResponse, String> {
    debug!("Fetching Azure resources: subscription={:?}, environment={:?}", subscription_id, environment);
    
    // Build cache key
    let cache_key = format!("azure_resources:{}:{}", 
        subscription_id.as_ref().unwrap_or(&"default".to_string()),
        environment.as_ref().unwrap_or(&"all".to_string()));
    
    // Try cache first
    let ttl = get_cache_ttl("azure_resources");
    if let Some(cached) = AZURE_RESOURCES_CACHE.get(&cache_key) {
        debug!("Cache hit for Azure resources: {}", cache_key);
        match serde_json::from_str::<CommandResponse>(&cached) {
            Ok(response) => return Ok(response),
            Err(_) => {
                // Cache entry corrupted, invalidate it
                AZURE_RESOURCES_CACHE.invalidate(&cache_key);
            }
        }
    }
    
    if !check_azure_cli_installed() {
        warn!("Azure CLI not installed when fetching resources");
        let winget_available = check_winget_available();
        let install_message = if winget_available {
            "Azure CLI is not installed. You can install it automatically using the 'Install Azure CLI' button, or manually from https://aka.ms/installazurecliwindows"
        } else {
            "Azure CLI is not installed. Please install it from https://aka.ms/installazurecliwindows"
        };
        
        return Ok(CommandResponse {
            success: false,
            result: Some(serde_json::json!({
                "azureCliMissing": true,
                "wingetAvailable": winget_available,
            })),
            message: None,
            error: Some(install_message.to_string()),
        });
    }

    // Apply rate limiting
    wait_azure_rate_limit().await;
    
    let (az_path, use_direct_path) = get_azure_cli_path();

    // Set subscription if provided
    if let Some(sub_id) = subscription_id {
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
    }

    // List resources using Azure CLI directly
    let output = if use_direct_path {
        Command::new("powershell")
            .arg("-NoProfile")
            .arg("-Command")
            .arg(format!("& '{}' resource list --output json", az_path.replace("'", "''")))
            .output()
    } else {
        Command::new("az")
            .arg("resource")
            .arg("list")
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
                        // Filter by environment if provided
                        let filter_applied = environment.is_some();
                        let filtered_resources: Vec<&serde_json::Value> = if let Some(env) = &environment {
                            resources_vec.iter().filter(|r| {
                                let name = r.get("name").and_then(|v| v.as_str()).unwrap_or("").to_lowercase();
                                let resource_group = r.get("resourceGroup").and_then(|v| v.as_str()).unwrap_or("").to_lowercase();
                                
                                let name_matches = name.contains(&env.to_lowercase()) || name.starts_with(&format!("{}-", env.to_lowercase()));
                                let rg_matches = resource_group.contains(&env.to_lowercase()) || resource_group.starts_with(&format!("{}-", env.to_lowercase()));
                                
                                let tags_match = r.get("tags").and_then(|t| {
                                    t.as_object().and_then(|tags_obj| {
                                        tags_obj.values().find(|v| {
                                            v.as_str().map(|s| s.to_lowercase().contains(&env.to_lowercase())).unwrap_or(false)
                                        })
                                    })
                                }).is_some();
                                
                                name_matches || rg_matches || tags_match
                            }).collect()
                        } else {
                            resources_vec.iter().collect()
                        };

                        let transformed: Vec<serde_json::Value> = filtered_resources.iter().map(|r| {
                            serde_json::json!({
                                "id": r.get("id").and_then(|v| v.as_str()).unwrap_or(""),
                                "name": r.get("name").and_then(|v| v.as_str()).unwrap_or(""),
                                "type": r.get("type").and_then(|v| v.as_str()).unwrap_or(""),
                                "location": r.get("location").and_then(|v| v.as_str()),
                                "resourceGroup": r.get("resourceGroup").and_then(|v| v.as_str()),
                                "sku": r.get("sku"),
                                "kind": r.get("kind").and_then(|v| v.as_str()),
                                "tags": r.get("tags"),
                            })
                        }).collect();

                        info!("Successfully fetched {} Azure resources (filtered: {}, filter_applied: {})", 
                            resources_vec.len(), 
                            transformed.len(),
                            filter_applied);

                        let response = CommandResponse {
                            success: true,
                            result: Some(serde_json::json!(transformed)),
                            message: Some(format!("Found {} resources", transformed.len())),
                            error: None,
                        };
                        
                        // Cache the response
                        if let Ok(cached_json) = serde_json::to_string(&response) {
                            AZURE_RESOURCES_CACHE.set(cache_key.clone(), cached_json, ttl);
                        }

                        Ok(response)
                    }
                    Err(e) => {
                        error!("Failed to parse Azure CLI response: {}", e);
                        Ok(CommandResponse {
                            success: false,
                            result: None,
                            message: None,
                            error: Some(format!("Failed to parse Azure CLI response: {}. Output: {}", e, stdout)),
                        })
                    },
                }
            } else {
                let stderr = String::from_utf8_lossy(&result.stderr);
                let stdout = String::from_utf8_lossy(&result.stdout);
                error!("Azure CLI command failed: {}\n{}", stderr, stdout);
                Ok(CommandResponse {
                    success: false,
                    result: None,
                    message: None,
                    error: Some(format!("Azure CLI error: {}\n{}", stderr, stdout)),
                })
            }
        }
        Err(e) => {
            error!("Failed to execute Azure CLI command: {}", e);
            Ok(CommandResponse {
                success: false,
                result: None,
                message: None,
                error: Some(format!("Failed to execute Azure CLI: {}", e)),
            })
        },
    }
}

/// Delete an Azure resource by resource ID
#[tauri::command]
pub async fn delete_azure_resource(resource_id: String) -> Result<CommandResponse, String> {
    if !check_azure_cli_installed() {
        let winget_available = check_winget_available();
        let install_message = if winget_available {
            "Azure CLI is not installed. You can install it automatically using the 'Install Azure CLI' button, or manually from https://aka.ms/installazurecliwindows"
        } else {
            "Azure CLI is not installed. Please install it from https://aka.ms/installazurecliwindows"
        };
        
        return Ok(CommandResponse {
            success: false,
            result: Some(serde_json::json!({
                "azureCliMissing": true,
                "wingetAvailable": winget_available,
            })),
            message: None,
            error: Some(install_message.to_string()),
        });
    }

    // Extract resource group and resource name from resource ID
    let parts: Vec<&str> = resource_id.split('/').collect();
    let mut resource_group = String::new();
    let mut resource_name = String::new();
    
    for (i, part) in parts.iter().enumerate() {
        if part == &"resourceGroups" && i + 1 < parts.len() {
            resource_group = parts[i + 1].to_string();
        }
        if i > 0 && parts[i - 1] == "providers" && i < parts.len() {
            if i + 1 < parts.len() {
                resource_name = parts[i + 1].to_string();
            }
        }
    }

    if resource_group.is_empty() || resource_name.is_empty() {
        return Ok(CommandResponse {
            success: false,
            result: None,
            message: None,
            error: Some(format!("Invalid resource ID format: {}", resource_id)),
        });
    }

    let (az_path, use_direct_path) = get_azure_cli_path();

    // Azure CLI resource delete doesn't support --yes flag in some versions
    // Remove the flag and let it run (it may prompt, but in non-interactive mode it should proceed)
    let delete_output = if use_direct_path {
        Command::new("powershell")
            .arg("-NoProfile")
            .arg("-Command")
            .arg(format!("& '{}' resource delete --ids '{}'", az_path.replace("'", "''"), resource_id.replace("'", "''")))
            .output()
    } else {
        Command::new("az")
            .arg("resource")
            .arg("delete")
            .arg("--ids")
            .arg(&resource_id)
            .output()
    };

    match delete_output {
        Ok(output) => {
            if output.status.success() {
                info!("Successfully deleted Azure resource: {}", resource_name);
                Ok(CommandResponse {
                    success: true,
                    result: Some(serde_json::json!({
                        "message": format!("Resource {} deleted successfully", resource_name)
                    })),
                    message: Some(format!("Resource deleted successfully")),
                    error: None,
                })
            } else {
                let error_msg = String::from_utf8_lossy(&output.stderr);
                error!("Failed to delete Azure resource {}: {}", resource_name, error_msg);
                Ok(CommandResponse {
                    success: false,
                    result: None,
                    message: None,
                    error: Some(format!("Failed to delete resource: {}", error_msg)),
                })
            }
        }
        Err(e) => {
            error!("Failed to execute Azure resource delete command: {}", e);
            Ok(CommandResponse {
                success: false,
                result: None,
                message: None,
                error: Some(format!("Failed to delete resource: {}", e)),
            })
        },
    }
}

/// Check if current user is a subscription owner
#[tauri::command]
pub async fn check_subscription_owner() -> Result<CommandResponse, String> {
    if !check_azure_cli_installed() {
        return Ok(CommandResponse {
            success: false,
            result: None,
            message: None,
            error: Some("Azure CLI is not installed".to_string()),
        });
    }

    let (az_path, use_direct_path) = get_azure_cli_path();

    // Get current user info
    let account_output = if use_direct_path {
        Command::new("powershell")
            .arg("-NoProfile")
            .arg("-Command")
            .arg(format!("& '{}' account show --query user.name --output tsv", az_path.replace("'", "''")))
            .output()
    } else {
        Command::new("az")
            .arg("account")
            .arg("show")
            .arg("--query")
            .arg("user.name")
            .arg("--output")
            .arg("tsv")
            .output()
    };

    let user_name = match account_output {
        Ok(result) => {
            if result.status.success() {
                String::from_utf8_lossy(&result.stdout).trim().to_string()
            } else {
                return Ok(CommandResponse {
                    success: false,
                    result: None,
                    message: None,
                    error: Some("Failed to get current user".to_string()),
                });
            }
        }
        Err(e) => {
            return Ok(CommandResponse {
                success: false,
                result: None,
                message: None,
                error: Some(format!("Failed to execute Azure CLI: {}", e)),
            });
        }
    };

    let sub_id = match get_azure_subscription_id() {
        Ok(id) => id,
        Err(e) => {
            return Ok(CommandResponse {
                success: false,
                result: None,
                message: None,
                error: Some(e),
            });
        }
    };

    // Check role assignments for Owner role
    let role_check = if use_direct_path {
        Command::new("powershell")
            .arg("-NoProfile")
            .arg("-Command")
            .arg(format!("& '{}' role assignment list --scope /subscriptions/{} --query \"[?principalName=='{}' && roleDefinitionName=='Owner']\" --output json", az_path.replace("'", "''"), sub_id.replace("'", "''"), user_name.replace("'", "''")))
            .output()
    } else {
        Command::new("az")
            .arg("role")
            .arg("assignment")
            .arg("list")
            .arg("--scope")
            .arg(format!("/subscriptions/{}", sub_id))
            .arg("--query")
            .arg(format!("[?principalName=='{}' && roleDefinitionName=='Owner']", user_name))
            .arg("--output")
            .arg("json")
            .output()
    };

    match role_check {
        Ok(result) => {
            if result.status.success() {
                let stdout = String::from_utf8_lossy(&result.stdout);
                let assignments: Result<Vec<serde_json::Value>, _> = serde_json::from_str(&stdout);
                
                match assignments {
                    Ok(assignments_vec) => {
                        let is_owner = !assignments_vec.is_empty();
                        Ok(CommandResponse {
                            success: true,
                            result: Some(serde_json::json!({
                                "isOwner": is_owner,
                                "userName": user_name,
                                "subscriptionId": sub_id,
                            })),
                            message: None,
                            error: None,
                        })
                    }
                    Err(e) => Ok(CommandResponse {
                        success: false,
                        result: None,
                        message: None,
                        error: Some(format!("Failed to parse role assignment response: {}", e)),
                    }),
                }
            } else {
                let stderr = String::from_utf8_lossy(&result.stderr);
                Ok(CommandResponse {
                    success: false,
                    result: None,
                    message: None,
                    error: Some(format!("Failed to check role assignment: {}", stderr)),
                })
            }
        }
        Err(e) => Ok(CommandResponse {
            success: false,
            result: None,
            message: None,
            error: Some(format!("Failed to execute Azure CLI: {}", e)),
        }),
    }
}
