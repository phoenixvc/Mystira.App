//! General utility commands.
//!
//! This module provides miscellaneous utility functions for:
//! - Repository operations (root detection, branch info)
//! - File operations (reading Bicep files)
//! - CLI build management
//! - Connection testing
//! - Resource health checks
//! - Window management
//!
//! These commands don't fit into specific domain modules and are commonly
//! used across the application.

use crate::types::CommandResponse;
use crate::cli::execute_devhub_cli;
use crate::helpers::{find_repo_root, get_cli_executable_path, check_azure_cli_installed};
use std::process::Command;
use std::path::PathBuf;
use std::fs;
use std::env;
use tauri::AppHandle;

/// Test a connection (via CLI)
#[tauri::command]
pub async fn test_connection(connection_type: String, connection_string: Option<String>) -> Result<CommandResponse, String> {
    let args = serde_json::json!({
        "type": connection_type,
        "connectionString": connection_string
    });
    execute_devhub_cli("connection.test".to_string(), args).await
}

/// Get the build time of the CLI executable
#[tauri::command]
pub async fn get_cli_build_time() -> Result<Option<i64>, String> {
    // Try to find the CLI executable
    match get_cli_executable_path() {
        Ok(path) => {
            // Get file metadata to find last modified time
            match std::fs::metadata(&path) {
                Ok(metadata) => {
                    if let Ok(modified) = metadata.modified() {
                        // Convert to timestamp (milliseconds since epoch)
                        let timestamp = modified
                            .duration_since(std::time::UNIX_EPOCH)
                            .map_err(|e| format!("Failed to calculate timestamp: {}", e))?
                            .as_millis() as i64;
                        Ok(Some(timestamp))
                    } else {
                        Ok(None)
                    }
                }
                Err(e) => Err(format!("Failed to get file metadata: {}", e)),
            }
        }
        Err(_) => Ok(None), // CLI not found, return None
    }
}

/// Build the DevHub CLI
#[tauri::command]
pub async fn build_cli() -> Result<CommandResponse, String> {
    // Find repo root
    let repo_root = find_repo_root()?;
    
    // Path to CLI project
    let cli_project_path = repo_root.join("tools/Mystira.DevHub.CLI/Mystira.DevHub.CLI.csproj");
    
    if !cli_project_path.exists() {
        return Err(format!(
            "CLI project not found at: {}\n\nPlease ensure you're running from the repository root.",
            cli_project_path.display()
        ));
    }
    
    // Build the CLI using dotnet build
    let output = Command::new("dotnet")
        .arg("build")
        .arg(&cli_project_path)
        .arg("--configuration")
        .arg("Debug")
        .arg("--no-incremental")
        .current_dir(repo_root.join("tools/Mystira.DevHub.CLI"))
        .output()
        .map_err(|e| format!("Failed to execute dotnet build: {}", e))?;
    
    let stdout = String::from_utf8_lossy(&output.stdout);
    let stderr = String::from_utf8_lossy(&output.stderr);
    
    // Combine stdout and stderr for full build output
    let full_output = if stderr.is_empty() {
        stdout.to_string()
    } else if stdout.is_empty() {
        stderr.to_string()
    } else {
        format!("{}\n{}", stdout, stderr)
    };
    
    if output.status.success() {
        // After successful build, get the build time from the file we just built
        // Use the repo_root we already found - the file is at:
        // repo_root/tools/Mystira.DevHub.CLI/bin/Debug/net9.0/Mystira.DevHub.CLI.exe (or .dll)
        let exe_path = repo_root.join("tools/Mystira.DevHub.CLI/bin/Debug/net9.0/Mystira.DevHub.CLI.exe");
        let dll_path = repo_root.join("tools/Mystira.DevHub.CLI/bin/Debug/net9.0/Mystira.DevHub.CLI.dll");
        
        // Wait a moment for file system to sync
        tokio::time::sleep(tokio::time::Duration::from_millis(1500)).await;
        
        // Try multiple times in case file system is slow
        let mut build_time = None;
        for attempt in 0..5 {
            // Try .exe first, then .dll
            for path in &[&exe_path, &dll_path] {
                if path.exists() {
                    if let Ok(metadata) = std::fs::metadata(path) {
                        if let Ok(modified) = metadata.modified() {
                            build_time = Some(modified
                                .duration_since(std::time::UNIX_EPOCH)
                                .unwrap_or_default()
                                .as_millis() as i64);
                            break;
                        }
                    }
                }
            }
            
            if build_time.is_some() {
                break;
            }
            
            if attempt < 4 {
                tokio::time::sleep(tokio::time::Duration::from_millis(500)).await;
            }
        }
        
        // Final fallback: use get_cli_executable_path if we still haven't found it
        if build_time.is_none() {
            if let Ok(found_path) = get_cli_executable_path() {
                if let Ok(metadata) = std::fs::metadata(&found_path) {
                    if let Ok(modified) = metadata.modified() {
                        build_time = Some(modified
                            .duration_since(std::time::UNIX_EPOCH)
                            .unwrap_or_default()
                            .as_millis() as i64);
                    }
                }
            }
        }
        
        Ok(CommandResponse {
            success: true,
            message: Some(format!("CLI built successfully!")),
            result: Some(serde_json::json!({ 
                "output": full_output,
                "buildTime": build_time
            })),
            error: None,
        })
    } else {
        Ok(CommandResponse {
            success: false,
            message: None,
            result: Some(serde_json::json!({ "output": full_output })),
            error: Some(format!(
                "Build failed with exit code: {:?}",
                output.status.code()
            )),
        })
    }
}

/// Read a Bicep file from the repository
#[tauri::command]
pub async fn read_bicep_file(relative_path: String) -> Result<String, String> {
    // Find repo root
    let repo_root = find_repo_root()?;
    
    // Normalize path separators (handle both / and \)
    let normalized_path = relative_path.replace('/', std::path::MAIN_SEPARATOR.to_string().as_str());
    
    // Resolve the file path relative to repo root
    let file_path = repo_root.join(&normalized_path);
    
    // Check if file exists first (before canonicalizing)
    if !file_path.exists() {
        return Err(format!("File not found: {} (resolved to: {})", relative_path, file_path.display()));
    }
    
    // Security: Ensure the path is within the repo root (prevent directory traversal)
    // Normalize paths to handle different separators and symlinks
    let repo_root_canonical = repo_root.canonicalize()
        .map_err(|e| format!("Failed to canonicalize repo root: {}", e))?;
    let file_path_canonical = file_path.canonicalize()
        .map_err(|e| format!("Failed to canonicalize file path: {} - {}", file_path.display(), e))?;
    
    if !file_path_canonical.starts_with(&repo_root_canonical) {
        return Err(format!("Invalid path: path must be within repository root"));
    }
    
    // Read the file
    fs::read_to_string(&file_path)
        .map_err(|e| format!("Failed to read file {}: {}", relative_path, e))
}

/// Get the repository root path
#[tauri::command]
pub async fn get_repo_root() -> Result<String, String> {
    find_repo_root()
        .map(|p| p.to_string_lossy().to_string())
}

/// Get the current Git branch
#[tauri::command]
pub async fn get_current_branch(repo_root: String) -> Result<String, String> {
    let output = Command::new("git")
        .args(&["rev-parse", "--abbrev-ref", "HEAD"])
        .current_dir(&repo_root)
        .output()
        .map_err(|e| format!("Failed to get current branch: {}", e))?;
    
    if !output.status.success() {
        return Err("Not a git repository or git command failed".to_string());
    }
    
    let branch = String::from_utf8_lossy(&output.stdout).trim().to_string();
    Ok(branch)
}

/// Check the health endpoint of an Azure resource
#[tauri::command]
pub async fn check_resource_health_endpoint(
    resource_type: String,
    resource_name: String,
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
    
    let program_files = env::var("ProgramFiles").unwrap_or_else(|_| "C:\\Program Files".to_string());
    let az_path = format!("{}\\Microsoft SDKs\\Azure\\CLI2\\wbin\\az.cmd", program_files);
    let az_path_buf = PathBuf::from(&az_path);
    let use_direct_path = az_path_buf.exists();
    
    let mut health_status = "unknown".to_string();
    let mut health_details = serde_json::json!({});
    
    // Check App Service health endpoint
    if resource_type == "Microsoft.Web/sites" {
        // Get App Service URL
        let output = if use_direct_path {
            Command::new("powershell")
                .arg("-NoProfile")
                .arg("-Command")
                .arg(format!(
                    "& '{}' webapp show --name '{}' --resource-group '{}' --query defaultHostName --output tsv",
                    az_path.replace("'", "''"),
                    resource_name.replace("'", "''"),
                    resource_group.replace("'", "''")
                ))
                .output()
        } else {
            Command::new("az")
                .arg("webapp")
                .arg("show")
                .arg("--name")
                .arg(&resource_name)
                .arg("--resource-group")
                .arg(&resource_group)
                .arg("--query")
                .arg("defaultHostName")
                .arg("--output")
                .arg("tsv")
                .output()
        };
        
        match output {
            Ok(result) => {
                if result.status.success() {
                    let hostname = String::from_utf8_lossy(&result.stdout).trim().to_string();
                    if hostname.is_empty() {
                        return Ok(CommandResponse {
                            success: false,
                            result: None,
                            message: None,
                            error: Some("Failed to get App Service hostname: hostname is empty".to_string()),
                        });
                    }
                    
                    // Validate hostname format (basic check - must contain a dot)
                    if !hostname.contains('.') {
                        return Ok(CommandResponse {
                            success: false,
                            result: None,
                            message: None,
                            error: Some(format!("Invalid hostname format: {}", hostname)),
                        });
                    }
                    
                    let health_url = format!("https://{}/health", hostname);
                    
                    // Try to make HTTP request to health endpoint
                    let health_check = reqwest::Client::builder()
                        .timeout(std::time::Duration::from_secs(10))
                        .build();
                    
                    if let Ok(client) = health_check {
                        match client.get(&health_url).send().await {
                            Ok(response) => {
                                let status_code = response.status().as_u16();
                                if status_code == 200 {
                                    health_status = "healthy".to_string();
                                    if let Ok(body) = response.text().await {
                                        health_details = serde_json::json!({
                                            "statusCode": status_code,
                                            "response": body
                                        });
                                    }
                                } else if status_code >= 500 {
                                    health_status = "unhealthy".to_string();
                                } else {
                                    health_status = "degraded".to_string();
                                }
                                health_details["statusCode"] = serde_json::json!(status_code);
                            }
                            Err(e) => {
                                health_status = "unhealthy".to_string();
                                health_details = serde_json::json!({
                                    "error": format!("Failed to reach health endpoint: {}", e)
                                });
                            }
                        }
                    }
                } else {
                    let stderr = String::from_utf8_lossy(&result.stderr);
                    return Ok(CommandResponse {
                        success: false,
                        result: None,
                        message: None,
                        error: Some(format!("Failed to get App Service hostname: {}", stderr)),
                    });
                }
            }
            Err(e) => {
                return Ok(CommandResponse {
                    success: false,
                    result: None,
                    message: None,
                    error: Some(format!("Failed to get App Service hostname: {}", e)),
                });
            }
        }
    }
    
    // For other resource types, we could add more checks here
    // For now, return the health status
    
    Ok(CommandResponse {
        success: true,
        result: Some(serde_json::json!({
            "health": health_status,
            "details": health_details
        })),
        message: None,
        error: None,
    })
}

/// Create a new webview window
#[tauri::command]
pub async fn create_webview_window(
    url: String,
    title: String,
    app_handle: AppHandle,
) -> Result<(), String> {
    // Create a new window with the URL using Tauri v1 API
    let window_label = format!("webview-{}", title.replace(" ", "-").to_lowercase());
    
    tauri::WindowBuilder::new(
        &app_handle,
        &window_label,
        tauri::WindowUrl::External(url.parse().map_err(|e| format!("Invalid URL: {}", e))?)
    )
    .title(&title)
    .inner_size(1200.0, 800.0)
    .resizable(true)
    .build()
    .map_err(|e| format!("Failed to create window: {}", e))?;
    
    Ok(())
}
