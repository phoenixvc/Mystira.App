// Deploy Now commands - Smart deployment functionality
// Mirrors the logic from .deploy-now.ps1 and .deploy-config.ps1

use crate::helpers::get_azure_cli_path;
use crate::types::CommandResponse;
use serde_json::json;
use std::process::Command;
use tracing::{info, warn, debug};

/// Check Azure login status
#[tauri::command]
pub async fn check_azure_login() -> Result<CommandResponse, String> {
    let (az_path, use_direct_path) = get_azure_cli_path();

    let result = if use_direct_path {
        Command::new("powershell")
            .arg("-NoProfile")
            .arg("-Command")
            .arg(format!(
                "& '{}' account show --output json",
                az_path.replace("'", "''")
            ))
            .output()
    } else {
        Command::new("az")
            .arg("account")
            .arg("show")
            .arg("--output")
            .arg("json")
            .output()
    };

    match result {
        Ok(output) => {
            if output.status.success() {
                let stdout = String::from_utf8_lossy(&output.stdout);
                if let Ok(account) = serde_json::from_str::<serde_json::Value>(&stdout) {
                    Ok(CommandResponse {
                        success: true,
                        result: Some(json!({
                            "name": account.get("name").and_then(|v| v.as_str()).unwrap_or("Unknown"),
                            "id": account.get("id").and_then(|v| v.as_str()).unwrap_or("Unknown"),
                        })),
                        message: Some("Logged in to Azure".to_string()),
                        error: None,
                    })
                } else {
                    Ok(CommandResponse {
                        success: false,
                        result: None,
                        message: None,
                        error: Some("Failed to parse Azure account info".to_string()),
                    })
                }
            } else {
                Ok(CommandResponse {
                    success: false,
                    result: None,
                    message: None,
                    error: Some("Not logged in to Azure".to_string()),
                })
            }
        }
        Err(e) => Ok(CommandResponse {
            success: false,
            result: None,
            message: None,
            error: Some(format!("Failed to check Azure login: {}", e)),
        }),
    }
}

/// Check if GitHub PAT is configured
#[tauri::command]
pub async fn check_github_pat() -> Result<CommandResponse, String> {
    // Check environment variable
    let pat = std::env::var("GITHUB_PAT").ok();

    if let Some(token) = pat {
        if token.len() >= 20 {
            Ok(CommandResponse {
                success: true,
                result: Some(json!({
                    "configured": true,
                    "length": token.len(),
                })),
                message: Some("GitHub PAT is configured".to_string()),
                error: None,
            })
        } else {
            Ok(CommandResponse {
                success: false,
                result: Some(json!({ "configured": false })),
                message: None,
                error: Some("GitHub PAT appears too short".to_string()),
            })
        }
    } else {
        Ok(CommandResponse {
            success: false,
            result: Some(json!({ "configured": false })),
            message: None,
            error: Some("GitHub PAT not configured".to_string()),
        })
    }
}

/// Check if SWA CLI is installed
#[tauri::command]
pub async fn check_swa_cli() -> Result<CommandResponse, String> {
    // Try multiple methods to find SWA CLI
    let result = Command::new("swa")
        .arg("--version")
        .output();

    match result {
        Ok(output) if output.status.success() => {
            let version = String::from_utf8_lossy(&output.stdout).trim().to_string();
            Ok(CommandResponse {
                success: true,
                result: Some(json!({
                    "installed": true,
                    "version": version,
                })),
                message: Some("SWA CLI is installed".to_string()),
                error: None,
            })
        }
        _ => {
            // Try npm list as backup
            let npm_result = Command::new("npm")
                .arg("list")
                .arg("-g")
                .arg("@azure/static-web-apps-cli")
                .arg("--depth=0")
                .output();

            match npm_result {
                Ok(output) => {
                    let stdout = String::from_utf8_lossy(&output.stdout);
                    if stdout.contains("@azure/static-web-apps-cli") {
                        Ok(CommandResponse {
                            success: true,
                            result: Some(json!({
                                "installed": true,
                                "via": "npm",
                            })),
                            message: Some("SWA CLI is installed via npm".to_string()),
                            error: None,
                        })
                    } else {
                        Ok(CommandResponse {
                            success: false,
                            result: Some(json!({ "installed": false })),
                            message: None,
                            error: Some("SWA CLI not installed".to_string()),
                        })
                    }
                }
                Err(_) => Ok(CommandResponse {
                    success: false,
                    result: Some(json!({ "installed": false })),
                    message: None,
                    error: Some("SWA CLI not installed".to_string()),
                }),
            }
        }
    }
}

/// Check if npm is installed
#[tauri::command]
pub async fn check_npm() -> Result<CommandResponse, String> {
    let result = Command::new("npm")
        .arg("--version")
        .output();

    match result {
        Ok(output) if output.status.success() => {
            let version = String::from_utf8_lossy(&output.stdout).trim().to_string();
            Ok(CommandResponse {
                success: true,
                result: Some(json!(version)),
                message: Some(format!("npm v{} is installed", version)),
                error: None,
            })
        }
        _ => Ok(CommandResponse {
            success: false,
            result: None,
            message: None,
            error: Some("npm not installed".to_string()),
        }),
    }
}

/// Scan for existing Azure resources (resource groups and static web apps)
#[tauri::command]
pub async fn scan_existing_resources() -> Result<CommandResponse, String> {
    let (az_path, use_direct_path) = get_azure_cli_path();

    info!("Scanning for existing Mystira resources...");

    let mut resource_groups: Vec<serde_json::Value> = Vec::new();
    let mut static_web_apps: Vec<serde_json::Value> = Vec::new();

    // Scan resource groups
    let rg_result = if use_direct_path {
        Command::new("powershell")
            .arg("-NoProfile")
            .arg("-Command")
            .arg(format!(
                "& '{}' group list --query \"[?contains(name, 'mystira')]\" --output json",
                az_path.replace("'", "''")
            ))
            .output()
    } else {
        Command::new("az")
            .arg("group")
            .arg("list")
            .arg("--query")
            .arg("[?contains(name, 'mystira')]")
            .arg("--output")
            .arg("json")
            .output()
    };

    if let Ok(output) = rg_result {
        if output.status.success() {
            let stdout = String::from_utf8_lossy(&output.stdout);
            if let Ok(rgs) = serde_json::from_str::<Vec<serde_json::Value>>(&stdout) {
                for rg in rgs {
                    let name = rg.get("name").and_then(|v| v.as_str()).unwrap_or("");
                    let location = rg.get("location").and_then(|v| v.as_str()).unwrap_or("");

                    // Get resource count for this group
                    let count_result = if use_direct_path {
                        Command::new("powershell")
                            .arg("-NoProfile")
                            .arg("-Command")
                            .arg(format!(
                                "& '{}' resource list --resource-group '{}' --query 'length(@)' --output json",
                                az_path.replace("'", "''"),
                                name.replace("'", "''")
                            ))
                            .output()
                    } else {
                        Command::new("az")
                            .arg("resource")
                            .arg("list")
                            .arg("--resource-group")
                            .arg(name)
                            .arg("--query")
                            .arg("length(@)")
                            .arg("--output")
                            .arg("json")
                            .output()
                    };

                    let resource_count = count_result
                        .ok()
                        .and_then(|o| String::from_utf8(o.stdout).ok())
                        .and_then(|s| s.trim().parse::<i64>().ok())
                        .unwrap_or(0);

                    resource_groups.push(json!({
                        "name": name,
                        "location": location,
                        "hasResources": resource_count > 0,
                        "resourceCount": resource_count,
                    }));
                }
            }
        }
    }

    // Scan Static Web Apps
    let swa_result = if use_direct_path {
        Command::new("powershell")
            .arg("-NoProfile")
            .arg("-Command")
            .arg(format!(
                "& '{}' staticwebapp list --query \"[?contains(name, 'mystira')]\" --output json",
                az_path.replace("'", "''")
            ))
            .output()
    } else {
        Command::new("az")
            .arg("staticwebapp")
            .arg("list")
            .arg("--query")
            .arg("[?contains(name, 'mystira')]")
            .arg("--output")
            .arg("json")
            .output()
    };

    if let Ok(output) = swa_result {
        if output.status.success() {
            let stdout = String::from_utf8_lossy(&output.stdout);
            if let Ok(swas) = serde_json::from_str::<Vec<serde_json::Value>>(&stdout) {
                for swa in swas {
                    let name = swa.get("name").and_then(|v| v.as_str()).unwrap_or("");
                    let rg = swa.get("resourceGroup").and_then(|v| v.as_str()).unwrap_or("");
                    let location = swa.get("location").and_then(|v| v.as_str()).unwrap_or("");
                    let hostname = swa.get("defaultHostname").and_then(|v| v.as_str()).unwrap_or("");

                    static_web_apps.push(json!({
                        "name": name,
                        "resourceGroup": rg,
                        "location": location,
                        "defaultHostname": hostname,
                    }));
                }
            }
        }
    }

    info!("Found {} resource groups and {} static web apps", resource_groups.len(), static_web_apps.len());

    Ok(CommandResponse {
        success: true,
        result: Some(json!({
            "resourceGroups": resource_groups,
            "staticWebApps": static_web_apps,
        })),
        message: Some(format!(
            "Found {} resource groups and {} static web apps",
            resource_groups.len(),
            static_web_apps.len()
        )),
        error: None,
    })
}

/// Get git repository status
#[tauri::command]
pub async fn get_git_status(repo_root: String) -> Result<CommandResponse, String> {
    // Check if it's a git repository
    let git_dir = std::path::Path::new(&repo_root).join(".git");
    if !git_dir.exists() {
        return Ok(CommandResponse {
            success: false,
            result: Some(json!({ "isRepository": false })),
            message: None,
            error: Some("Not a git repository".to_string()),
        });
    }

    // Get current branch
    let branch_result = Command::new("git")
        .arg("rev-parse")
        .arg("--abbrev-ref")
        .arg("HEAD")
        .current_dir(&repo_root)
        .output();

    let branch = branch_result
        .ok()
        .and_then(|o| String::from_utf8(o.stdout).ok())
        .map(|s| s.trim().to_string())
        .unwrap_or_default();

    // Get uncommitted changes
    let status_result = Command::new("git")
        .arg("status")
        .arg("--porcelain")
        .current_dir(&repo_root)
        .output();

    let uncommitted_files: Vec<String> = status_result
        .ok()
        .and_then(|o| String::from_utf8(o.stdout).ok())
        .map(|s| {
            s.lines()
                .map(|l| l.trim().to_string())
                .filter(|l| !l.is_empty())
                .collect()
        })
        .unwrap_or_default();

    let has_uncommitted = !uncommitted_files.is_empty();

    // Get ahead/behind counts
    let fetch_result = Command::new("git")
        .arg("fetch")
        .arg("origin")
        .current_dir(&repo_root)
        .output();

    let ahead_result = Command::new("git")
        .arg("rev-list")
        .arg(format!("origin/{}..HEAD", branch))
        .arg("--count")
        .current_dir(&repo_root)
        .output();

    let ahead_count = ahead_result
        .ok()
        .and_then(|o| String::from_utf8(o.stdout).ok())
        .and_then(|s| s.trim().parse::<i64>().ok())
        .unwrap_or(0);

    let behind_result = Command::new("git")
        .arg("rev-list")
        .arg(format!("HEAD..origin/{}", branch))
        .arg("--count")
        .current_dir(&repo_root)
        .output();

    let behind_count = behind_result
        .ok()
        .and_then(|o| String::from_utf8(o.stdout).ok())
        .and_then(|s| s.trim().parse::<i64>().ok())
        .unwrap_or(0);

    Ok(CommandResponse {
        success: true,
        result: Some(json!({
            "isRepository": true,
            "branch": branch,
            "hasUncommittedChanges": has_uncommitted,
            "uncommittedFiles": uncommitted_files,
            "aheadCount": ahead_count,
            "behindCount": behind_count,
        })),
        message: None,
        error: None,
    })
}

/// Stage all git changes
#[tauri::command]
pub async fn git_stage_all(repo_root: String) -> Result<CommandResponse, String> {
    let result = Command::new("git")
        .arg("add")
        .arg(".")
        .current_dir(&repo_root)
        .output();

    match result {
        Ok(output) if output.status.success() => Ok(CommandResponse {
            success: true,
            result: None,
            message: Some("Changes staged".to_string()),
            error: None,
        }),
        Ok(output) => Ok(CommandResponse {
            success: false,
            result: None,
            message: None,
            error: Some(String::from_utf8_lossy(&output.stderr).to_string()),
        }),
        Err(e) => Ok(CommandResponse {
            success: false,
            result: None,
            message: None,
            error: Some(format!("Failed to stage changes: {}", e)),
        }),
    }
}

/// Commit git changes
#[tauri::command]
pub async fn git_commit(repo_root: String, message: String) -> Result<CommandResponse, String> {
    let result = Command::new("git")
        .arg("commit")
        .arg("-m")
        .arg(&message)
        .current_dir(&repo_root)
        .output();

    match result {
        Ok(output) if output.status.success() => Ok(CommandResponse {
            success: true,
            result: None,
            message: Some("Changes committed".to_string()),
            error: None,
        }),
        Ok(output) => {
            let stderr = String::from_utf8_lossy(&output.stderr);
            if stderr.contains("nothing to commit") {
                Ok(CommandResponse {
                    success: true,
                    result: None,
                    message: Some("Nothing to commit".to_string()),
                    error: None,
                })
            } else {
                Ok(CommandResponse {
                    success: false,
                    result: None,
                    message: None,
                    error: Some(stderr.to_string()),
                })
            }
        }
        Err(e) => Ok(CommandResponse {
            success: false,
            result: None,
            message: None,
            error: Some(format!("Failed to commit: {}", e)),
        }),
    }
}

/// Create empty git commit
#[tauri::command]
pub async fn git_commit_empty(repo_root: String, message: String) -> Result<CommandResponse, String> {
    let result = Command::new("git")
        .arg("commit")
        .arg("--allow-empty")
        .arg("-m")
        .arg(&message)
        .current_dir(&repo_root)
        .output();

    match result {
        Ok(output) if output.status.success() => Ok(CommandResponse {
            success: true,
            result: None,
            message: Some("Empty commit created".to_string()),
            error: None,
        }),
        Ok(output) => Ok(CommandResponse {
            success: false,
            result: None,
            message: None,
            error: Some(String::from_utf8_lossy(&output.stderr).to_string()),
        }),
        Err(e) => Ok(CommandResponse {
            success: false,
            result: None,
            message: None,
            error: Some(format!("Failed to create empty commit: {}", e)),
        }),
    }
}

/// Push git branch
#[tauri::command]
pub async fn git_push(repo_root: String, branch: String) -> Result<CommandResponse, String> {
    let result = Command::new("git")
        .arg("push")
        .arg("origin")
        .arg(&branch)
        .current_dir(&repo_root)
        .output();

    match result {
        Ok(output) if output.status.success() => Ok(CommandResponse {
            success: true,
            result: None,
            message: Some(format!("Pushed to origin/{}", branch)),
            error: None,
        }),
        Ok(output) => Ok(CommandResponse {
            success: false,
            result: None,
            message: None,
            error: Some(String::from_utf8_lossy(&output.stderr).to_string()),
        }),
        Err(e) => Ok(CommandResponse {
            success: false,
            result: None,
            message: None,
            error: Some(format!("Failed to push: {}", e)),
        }),
    }
}

/// Sync git repository (fetch and pull)
#[tauri::command]
pub async fn git_sync(repo_root: String, branch: String) -> Result<CommandResponse, String> {
    // Fetch
    let fetch_result = Command::new("git")
        .arg("fetch")
        .arg("origin")
        .current_dir(&repo_root)
        .output();

    if let Err(e) = fetch_result {
        return Ok(CommandResponse {
            success: false,
            result: None,
            message: None,
            error: Some(format!("Failed to fetch: {}", e)),
        });
    }

    // Pull
    let pull_result = Command::new("git")
        .arg("pull")
        .arg("origin")
        .arg(&branch)
        .current_dir(&repo_root)
        .output();

    match pull_result {
        Ok(output) if output.status.success() => Ok(CommandResponse {
            success: true,
            result: None,
            message: Some("Repository synced".to_string()),
            error: None,
        }),
        Ok(output) => {
            let stderr = String::from_utf8_lossy(&output.stderr);
            if stderr.contains("Already up to date") || stderr.is_empty() {
                Ok(CommandResponse {
                    success: true,
                    result: None,
                    message: Some("Already up to date".to_string()),
                    error: None,
                })
            } else {
                Ok(CommandResponse {
                    success: false,
                    result: None,
                    message: None,
                    error: Some(stderr.to_string()),
                })
            }
        }
        Err(e) => Ok(CommandResponse {
            success: false,
            result: None,
            message: None,
            error: Some(format!("Failed to sync: {}", e)),
        }),
    }
}

/// Update CORS settings on API services
#[tauri::command]
pub async fn update_cors_settings(
    resource_group: String,
    api_name: String,
    admin_api_name: Option<String>,
) -> Result<CommandResponse, String> {
    let (az_path, use_direct_path) = get_azure_cli_path();

    let cors_origins = "http://localhost:7000,https://localhost:7000,https://mystira.app,https://blue-water-0eab7991e.3.azurestaticapps.net,https://brave-meadow-0ecd87c03.3.azurestaticapps.net";

    info!("Updating CORS settings for {} in {}", api_name, resource_group);

    // Update main API
    let result = if use_direct_path {
        Command::new("powershell")
            .arg("-NoProfile")
            .arg("-Command")
            .arg(format!(
                "& '{}' webapp config appsettings set --name '{}' --resource-group '{}' --settings 'CorsSettings__AllowedOrigins={}' --output none",
                az_path.replace("'", "''"),
                api_name.replace("'", "''"),
                resource_group.replace("'", "''"),
                cors_origins
            ))
            .output()
    } else {
        Command::new("az")
            .arg("webapp")
            .arg("config")
            .arg("appsettings")
            .arg("set")
            .arg("--name")
            .arg(&api_name)
            .arg("--resource-group")
            .arg(&resource_group)
            .arg("--settings")
            .arg(format!("CorsSettings__AllowedOrigins={}", cors_origins))
            .arg("--output")
            .arg("none")
            .output()
    };

    if let Err(e) = result {
        return Ok(CommandResponse {
            success: false,
            result: None,
            message: None,
            error: Some(format!("Failed to update CORS for {}: {}", api_name, e)),
        });
    }

    // Update admin API if provided
    if let Some(admin_api) = admin_api_name {
        let admin_result = if use_direct_path {
            Command::new("powershell")
                .arg("-NoProfile")
                .arg("-Command")
                .arg(format!(
                    "& '{}' webapp config appsettings set --name '{}' --resource-group '{}' --settings 'CorsSettings__AllowedOrigins={}' --output none",
                    az_path.replace("'", "''"),
                    admin_api.replace("'", "''"),
                    resource_group.replace("'", "''"),
                    cors_origins
                ))
                .output()
        } else {
            Command::new("az")
                .arg("webapp")
                .arg("config")
                .arg("appsettings")
                .arg("set")
                .arg("--name")
                .arg(&admin_api)
                .arg("--resource-group")
                .arg(&resource_group)
                .arg("--settings")
                .arg(format!("CorsSettings__AllowedOrigins={}", cors_origins))
                .arg("--output")
                .arg("none")
                .output()
        };

        if let Err(e) = admin_result {
            warn!("Failed to update CORS for admin API: {}", e);
        }
    }

    Ok(CommandResponse {
        success: true,
        result: None,
        message: Some("CORS settings updated".to_string()),
        error: None,
    })
}

/// Restart API services
#[tauri::command]
pub async fn restart_api_services(
    resource_group: String,
    api_name: String,
    admin_api_name: Option<String>,
) -> Result<CommandResponse, String> {
    let (az_path, use_direct_path) = get_azure_cli_path();

    info!("Restarting API services in {}", resource_group);

    // Restart main API
    let result = if use_direct_path {
        Command::new("powershell")
            .arg("-NoProfile")
            .arg("-Command")
            .arg(format!(
                "& '{}' webapp restart --name '{}' --resource-group '{}' --output none",
                az_path.replace("'", "''"),
                api_name.replace("'", "''"),
                resource_group.replace("'", "''")
            ))
            .output()
    } else {
        Command::new("az")
            .arg("webapp")
            .arg("restart")
            .arg("--name")
            .arg(&api_name)
            .arg("--resource-group")
            .arg(&resource_group)
            .arg("--output")
            .arg("none")
            .output()
    };

    if let Err(e) = result {
        return Ok(CommandResponse {
            success: false,
            result: None,
            message: None,
            error: Some(format!("Failed to restart {}: {}", api_name, e)),
        });
    }

    // Restart admin API if provided
    if let Some(admin_api) = admin_api_name {
        let admin_result = if use_direct_path {
            Command::new("powershell")
                .arg("-NoProfile")
                .arg("-Command")
                .arg(format!(
                    "& '{}' webapp restart --name '{}' --resource-group '{}' --output none",
                    az_path.replace("'", "''"),
                    admin_api.replace("'", "''"),
                    resource_group.replace("'", "''")
                ))
                .output()
        } else {
            Command::new("az")
                .arg("webapp")
                .arg("restart")
                .arg("--name")
                .arg(&admin_api)
                .arg("--resource-group")
                .arg(&resource_group)
                .arg("--output")
                .arg("none")
                .output()
        };

        if let Err(e) = admin_result {
            warn!("Failed to restart admin API: {}", e);
        }
    }

    Ok(CommandResponse {
        success: true,
        result: None,
        message: Some("API services restarted".to_string()),
        error: None,
    })
}

/// Disconnect SWA built-in CI/CD
#[tauri::command]
pub async fn disconnect_swa_cicd(
    resource_group: String,
    swa_name: String,
) -> Result<CommandResponse, String> {
    let (az_path, use_direct_path) = get_azure_cli_path();

    info!("Disconnecting SWA CI/CD for {} in {}", swa_name, resource_group);

    let result = if use_direct_path {
        Command::new("powershell")
            .arg("-NoProfile")
            .arg("-Command")
            .arg(format!(
                "& '{}' staticwebapp disconnect --name '{}' --resource-group '{}'",
                az_path.replace("'", "''"),
                swa_name.replace("'", "''"),
                resource_group.replace("'", "''")
            ))
            .output()
    } else {
        Command::new("az")
            .arg("staticwebapp")
            .arg("disconnect")
            .arg("--name")
            .arg(&swa_name)
            .arg("--resource-group")
            .arg(&resource_group)
            .output()
    };

    match result {
        Ok(output) if output.status.success() => Ok(CommandResponse {
            success: true,
            result: None,
            message: Some("SWA CI/CD disconnected".to_string()),
            error: None,
        }),
        Ok(output) => {
            let stderr = String::from_utf8_lossy(&output.stderr);
            if stderr.contains("already disconnected") || stderr.is_empty() {
                Ok(CommandResponse {
                    success: true,
                    result: None,
                    message: Some("SWA CI/CD already disconnected".to_string()),
                    error: None,
                })
            } else {
                Ok(CommandResponse {
                    success: false,
                    result: None,
                    message: None,
                    error: Some(stderr.to_string()),
                })
            }
        }
        Err(e) => Ok(CommandResponse {
            success: false,
            result: None,
            message: None,
            error: Some(format!("Failed to disconnect SWA CI/CD: {}", e)),
        }),
    }
}

/// Get SWA deployment token
#[tauri::command]
pub async fn get_swa_deployment_token(
    resource_group: String,
    swa_name: String,
) -> Result<CommandResponse, String> {
    let (az_path, use_direct_path) = get_azure_cli_path();

    info!("Getting deployment token for {} in {}", swa_name, resource_group);

    let result = if use_direct_path {
        Command::new("powershell")
            .arg("-NoProfile")
            .arg("-Command")
            .arg(format!(
                "& '{}' staticwebapp secrets list --name '{}' --resource-group '{}' --query 'properties.apiKey' -o tsv",
                az_path.replace("'", "''"),
                swa_name.replace("'", "''"),
                resource_group.replace("'", "''")
            ))
            .output()
    } else {
        Command::new("az")
            .arg("staticwebapp")
            .arg("secrets")
            .arg("list")
            .arg("--name")
            .arg(&swa_name)
            .arg("--resource-group")
            .arg(&resource_group)
            .arg("--query")
            .arg("properties.apiKey")
            .arg("-o")
            .arg("tsv")
            .output()
    };

    match result {
        Ok(output) if output.status.success() => {
            let token = String::from_utf8_lossy(&output.stdout).trim().to_string();
            if !token.is_empty() {
                Ok(CommandResponse {
                    success: true,
                    result: Some(json!(token)),
                    message: Some("Deployment token retrieved".to_string()),
                    error: None,
                })
            } else {
                Ok(CommandResponse {
                    success: false,
                    result: None,
                    message: None,
                    error: Some("Empty token returned".to_string()),
                })
            }
        }
        Ok(output) => Ok(CommandResponse {
            success: false,
            result: None,
            message: None,
            error: Some(String::from_utf8_lossy(&output.stderr).to_string()),
        }),
        Err(e) => Ok(CommandResponse {
            success: false,
            result: None,
            message: None,
            error: Some(format!("Failed to get deployment token: {}", e)),
        }),
    }
}
