//! Service status and health check operations.
//!
//! This module provides functions to check service status and health.

use crate::types::{ServiceStatus, ServiceManager};
use crate::services::helpers::is_process_running;
use tauri::State;

/// Get status of all running services
#[tauri::command]
pub async fn get_service_status(
    services: State<'_, ServiceManager>,
) -> Result<Vec<ServiceStatus>, String> {
    let services_guard = services.lock().map_err(|e| format!("Lock error: {}", e))?;
    
    let mut statuses = Vec::new();
    for (_name, info) in services_guard.iter() {
        // Check if process is still running by PID
        let is_running = if let Some(pid) = info.pid {
            is_process_running(pid)
        } else {
            false
        };
        
        if is_running {
            statuses.push(ServiceStatus {
                name: info.name.clone(),
                running: true,
                port: Some(info.port),
                url: info.url.clone(),
            });
        }
    }
    
    Ok(statuses)
}

/// Check service health via HTTP request
#[tauri::command]
pub async fn check_service_health(url: String) -> Result<bool, String> {
    // Simple HTTP health check
    let client = reqwest::Client::builder()
        .timeout(std::time::Duration::from_secs(2))
        .danger_accept_invalid_certs(true) // For localhost self-signed certs
        .build()
        .map_err(|e| format!("Failed to create HTTP client: {}", e))?;
    
    match client.get(&url).send().await {
        Ok(response) => Ok(response.status().is_success()),
        Err(_) => Ok(false),
    }
}

