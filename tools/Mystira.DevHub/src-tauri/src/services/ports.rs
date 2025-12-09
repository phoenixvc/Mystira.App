//! Port management operations.
//!
//! This module provides functions for port discovery, availability checking,
//! and port configuration in launchSettings.json files.

use std::fs;
use std::process::Command;
use serde_json::Value;

/// Check if a port is available
#[tauri::command]
pub async fn check_port_available(port: u16) -> Result<bool, String> {
    #[cfg(target_os = "windows")]
    {
        let output = Command::new("powershell")
            .args(&[
                "-Command",
                &format!("Get-NetTCPConnection -LocalPort {} -ErrorAction SilentlyContinue | Measure-Object | Select-Object -ExpandProperty Count", port)
            ])
            .output()
            .map_err(|e| format!("Failed to check port: {}", e))?;
        
        let stdout = String::from_utf8_lossy(&output.stdout);
        let count: u32 = stdout.trim().parse().unwrap_or(0);
        Ok(count == 0)
    }
    
    #[cfg(not(target_os = "windows"))]
    {
        use std::net::TcpListener;
        match TcpListener::bind(format!("127.0.0.1:{}", port)) {
            Ok(_) => Ok(true),
            Err(_) => Ok(false),
        }
    }
}

/// Get the port configured for a service
#[tauri::command]
pub async fn get_service_port(service_name: String, repo_root: String) -> Result<u16, String> {
    let launch_settings_path = match service_name.as_str() {
        "api" => format!("{}\\src\\Mystira.App.Api\\Properties\\launchSettings.json", repo_root),
        "admin-api" => format!("{}\\src\\Mystira.App.Admin.Api\\Properties\\launchSettings.json", repo_root),
        "pwa" => format!("{}\\src\\Mystira.App.PWA\\Properties\\launchSettings.json", repo_root),
        _ => return Err(format!("Unknown service: {}", service_name)),
    };

    let content = fs::read_to_string(&launch_settings_path)
        .map_err(|e| format!("Failed to read launchSettings.json: {}", e))?;
    
    let json: Value = serde_json::from_str(&content)
        .map_err(|e| format!("Failed to parse launchSettings.json: {}", e))?;
    
    // Extract port from https profile
    if let Some(profiles) = json.get("profiles") {
        if let Some(https_profile) = profiles.get("https") {
            if let Some(app_url) = https_profile.get("applicationUrl").and_then(|v| v.as_str()) {
                // Parse "https://localhost:7096;http://localhost:5260"
                if let Some(https_part) = app_url.split(';').next() {
                    if let Some(port_str) = https_part.split(':').last() {
                        if let Ok(port) = port_str.parse::<u16>() {
                            return Ok(port);
                        }
                    }
                }
            }
        }
    }
    
    Err("Could not find port in launchSettings.json".to_string())
}

/// Update the port configured for a service
#[tauri::command]
pub async fn update_service_port(service_name: String, repo_root: String, new_port: u16) -> Result<(), String> {
    let launch_settings_path = match service_name.as_str() {
        "api" => format!("{}\\src\\Mystira.App.Api\\Properties\\launchSettings.json", repo_root),
        "admin-api" => format!("{}\\src\\Mystira.App.Admin.Api\\Properties\\launchSettings.json", repo_root),
        "pwa" => format!("{}\\src\\Mystira.App.PWA\\Properties\\launchSettings.json", repo_root),
        _ => return Err(format!("Unknown service: {}", service_name)),
    };

    let content = fs::read_to_string(&launch_settings_path)
        .map_err(|e| format!("Failed to read launchSettings.json: {}", e))?;
    
    let mut json: Value = serde_json::from_str(&content)
        .map_err(|e| format!("Failed to parse launchSettings.json: {}", e))?;
    
    // Update port in https profile
    if let Some(profiles) = json.get_mut("profiles") {
        if let Some(https_profile) = profiles.get_mut("https") {
            if let Some(app_url) = https_profile.get_mut("applicationUrl") {
                if let Some(url_str) = app_url.as_str() {
                    // Parse and update: "https://localhost:7096;http://localhost:5260"
                    let parts: Vec<&str> = url_str.split(';').collect();
                    let http_part = if parts.len() > 1 { parts[1] } else { "" };
                    let http_port = if !http_part.is_empty() {
                        http_part.split(':').last().unwrap_or("5260")
                    } else {
                        "5260"
                    };
                    
                    let new_url = format!("https://localhost:{};http://localhost:{}", new_port, http_port);
                    *app_url = Value::String(new_url);
                }
            }
        }
    }
    
    // Write back to file
    let updated_content = serde_json::to_string_pretty(&json)
        .map_err(|e| format!("Failed to serialize launchSettings.json: {}", e))?;
    
    fs::write(&launch_settings_path, updated_content)
        .map_err(|e| format!("Failed to write launchSettings.json: {}", e))?;
    
    Ok(())
}

/// Find an available port starting from a given port number
#[tauri::command]
pub async fn find_available_port(start_port: u16) -> Result<u16, String> {
    // Try ports starting from start_port, up to start_port + 100
    for port in start_port..(start_port + 100) {
        let available = check_port_available(port).await?;
        if available {
            return Ok(port);
        }
    }
    Err("Could not find available port".to_string())
}

