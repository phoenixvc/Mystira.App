//! Service lifecycle management (start, stop, prebuild).
//!
//! This module handles starting and stopping services, including process
//! management, log streaming, and build operations.

use crate::types::{ServiceStatus, ServiceInfo, ServiceManager};
use crate::services::helpers::{
    get_service_paths, stop_service_process, setup_log_streaming, 
    build_service, kill_process_by_pid, kill_process_by_port
};
use tracing::{info, warn, error, debug};
use std::path::PathBuf;
use std::sync::Arc;
use std::process::Stdio;
use tauri::{State, AppHandle};
use tokio::process::Command as TokioCommand;

/// Pre-build a service (build without starting)
#[tauri::command]
pub async fn prebuild_service(
    service_name: String,
    repo_root: String,
    app_handle: AppHandle,
    services: State<'_, ServiceManager>,
) -> Result<(), String> {
    if repo_root.is_empty() {
        return Err(format!("Repository root is empty. Please configure the repository root in DevHub."));
    }
    
    let repo_path = PathBuf::from(&repo_root);
    if !repo_path.exists() {
        return Err(format!("Repository root does not exist: {}", repo_root));
    }
    
    // Stop ALL services before building to avoid file locks on shared DLLs (like Domain.dll)
    let all_services = vec!["api", "admin-api", "pwa"];
    let mut services_to_stop: Vec<(String, Option<u32>, u16)> = Vec::new();
    
    // Collect all running services that need to be stopped
    {
        let services_guard = services.lock().map_err(|e| format!("Lock error: {}", e))?;
        for svc_name in &all_services {
            if let Some(info) = services_guard.get(*svc_name) {
                services_to_stop.push((svc_name.to_string(), info.pid, info.port));
            }
        }
    }
    
    // Stop all running services
    for (svc_name, pid_opt, port) in &services_to_stop {
        // Remove from services map first
        {
            let mut services_guard = services.lock().map_err(|e| format!("Lock error: {}", e))?;
            services_guard.remove(svc_name);
        }
        
        // Kill the process
        if let Some(pid_val) = *pid_opt {
            kill_process_by_pid(pid_val).await;
        } else {
            kill_process_by_port(*port).await;
        }
    }
    
    // Wait longer for all file handles to release (especially important for shared DLLs)
    if !services_to_stop.is_empty() {
        tokio::time::sleep(tokio::time::Duration::from_millis(3000)).await;
    }
    
    let (project_path, _port, _url) = get_service_paths(&service_name, &repo_path)?;
    
    if !project_path.exists() {
        return Err(format!("Project directory does not exist: {}", project_path.display()));
    }
    
    let project_path_str = project_path.to_string_lossy().to_string();
    
    // Additional wait for any remaining file handles to release
    tokio::time::sleep(tokio::time::Duration::from_millis(1000)).await;

    // Build with streaming output
    build_service(&project_path_str, &service_name, app_handle).await?;

    Ok(())
}

/// Start a service
#[tauri::command]
pub async fn start_service(
    service_name: String,
    repo_root: String,
    services: State<'_, ServiceManager>,
    app_handle: AppHandle,
) -> Result<ServiceStatus, String> {
    info!("Starting service: name={}, repo_root={}", service_name, repo_root);
    
    // Check if service is already running
    {
        let services_guard = services.lock().map_err(|e| {
            error!("Failed to acquire service manager lock: {}", e);
            format!("Lock error: {}", e)
        })?;
        if services_guard.contains_key(&service_name) {
            warn!("Service {} is already running", service_name);
            return Err(format!("Service {} is already running", service_name));
        }
    }

    if repo_root.is_empty() {
        error!("Repository root is empty for service: {}", service_name);
        return Err(format!("Repository root is empty. Please configure the repository root in DevHub."));
    }
    
    let repo_path = PathBuf::from(&repo_root);
    if !repo_path.exists() {
        error!("Repository root does not exist: {}", repo_root);
        return Err(format!("Repository root does not exist: {}", repo_root));
    }
    
    let (project_path, port, url) = get_service_paths(&service_name, &repo_path)?;
    
    if !project_path.exists() {
        return Err(format!("Project directory does not exist: {}", project_path.display()));
    }
    
    let project_path_str = project_path.to_string_lossy().to_string();

    // Build with streaming output
    build_service(&project_path_str, &service_name, app_handle.clone()).await?;

    // Start the service
    let mut child = TokioCommand::new("dotnet")
        .arg("run")
        .current_dir(&project_path_str)
        .stdout(Stdio::piped())
        .stderr(Stdio::piped())
        .spawn()
        .map_err(|e| format!("Failed to start {}: {} (path: {})", service_name, e, project_path_str))?;

    let pid = child.id();

    // Take stdout and stderr BEFORE moving child into spawn
    let stdout = child.stdout.take();
    let stderr = child.stderr.take();

    // Store service info
    let service_info = ServiceInfo {
        name: service_name.clone(),
        port,
        url: url.clone(),
        pid,
    };
    {
        let mut services_guard = services.lock().map_err(|e| format!("Lock error: {}", e))?;
        services_guard.insert(service_name.clone(), service_info.clone());
    }
    
    // Clone the Arc from the State before spawning
    let services_arc = Arc::clone(&*services);
    let service_name_clone = service_name.clone();
    
    // Spawn a task to wait for the process (keeps it alive)
    tokio::spawn(async move {
        let _ = child.wait().await;
        // Process exited, remove from services
        if let Ok(mut guard) = services_arc.lock() {
            guard.remove(&service_name_clone);
        }
    });

    // Setup log streaming for stdout/stderr
    setup_log_streaming(stdout, stderr, app_handle, service_name.clone(), "run");

    info!("Service {} started successfully on port {}", service_name, port);
    
    Ok(ServiceStatus {
        name: service_name,
        running: true,
        port: Some(port),
        url,
    })
}

/// Stop a running service
#[tauri::command]
pub async fn stop_service(
    service_name: String,
    services: State<'_, ServiceManager>,
) -> Result<(), String> {
    info!("Stopping service: name={}", service_name);
    
    let service_info;
    
    // Extract service info while holding the lock
    {
        let mut services_guard = services.lock().map_err(|e| {
            error!("Failed to acquire service manager lock for stop: {}", e);
            format!("Lock error: {}", e)
        })?;
        
        if let Some(info) = services_guard.remove(&service_name) {
            service_info = info;
            debug!("Service {} found: pid={:?}, port={}", service_name, service_info.pid, service_info.port);
        } else {
            warn!("Service {} is not running", service_name);
            return Err(format!("Service {} is not running", service_name));
        }
    }
    
    // Stop the process (no lock held)
    stop_service_process(&service_info).await;
    
    info!("Service {} stopped successfully", service_name);
    Ok(())
}

