//! Shared helper functions for service management.
//!
//! This module provides utility functions used across service lifecycle operations:
//! - Process termination (Windows/Unix)
//! - Log streaming setup
//! - Service path resolution
//! - Build output streaming

use crate::types::ServiceInfo;
use std::path::PathBuf;
use std::process::Command;
use tauri::{AppHandle, Manager};
use tokio::process::Command as TokioCommand;
use tokio::io::{AsyncBufReadExt, BufReader as TokioBufReader};
use std::process::Stdio;

/// Get project path, port, and URL for a service
pub fn get_service_paths(service_name: &str, repo_path: &PathBuf) -> Result<(PathBuf, u16, Option<String>), String> {
    match service_name {
        "api" => Ok((
            repo_path.join("src").join("Mystira.App.Api"),
            7096,
            Some("https://localhost:7096/swagger".to_string()),
        )),
        "admin-api" => Ok((
            repo_path.join("src").join("Mystira.App.Admin.Api"),
            7097,
            Some("https://localhost:7097/swagger".to_string()),
        )),
        "pwa" => Ok((
            repo_path.join("src").join("Mystira.App.PWA"),
            7000,
            Some("http://localhost:7000".to_string()),
        )),
        _ => Err(format!("Unknown service: {}", service_name)),
    }
}

/// Kill a process by PID (platform-specific)
pub async fn kill_process_by_pid(pid: u32) {
    #[cfg(target_os = "windows")]
    {
        let _ = Command::new("taskkill")
            .args(&["/F", "/PID", &pid.to_string()])
            .output();
        
        // Wait for process to terminate (up to 5 seconds)
        for _ in 0..50 {
            let check = Command::new("tasklist")
                .args(&["/FI", &format!("PID eq {}", pid)])
                .output();
            
            if let Ok(output) = check {
                let output_str = String::from_utf8_lossy(&output.stdout);
                if !output_str.contains(&pid.to_string()) {
                    break;
                }
            }
            tokio::time::sleep(tokio::time::Duration::from_millis(100)).await;
        }
    }
    
    #[cfg(not(target_os = "windows"))]
    {
        let _ = Command::new("kill")
            .args(&["-9", &pid.to_string()])
            .output();
        tokio::time::sleep(tokio::time::Duration::from_millis(1000)).await;
    }
}

/// Kill a process by port (Windows only, fallback)
pub async fn kill_process_by_port(port: u16) {
    #[cfg(target_os = "windows")]
    {
        let _ = Command::new("powershell")
            .args(&[
                "-Command",
                &format!("Get-NetTCPConnection -LocalPort {} -ErrorAction SilentlyContinue | Select-Object -ExpandProperty OwningProcess | ForEach-Object {{ Stop-Process -Id $_ -Force }}", port)
            ])
            .output();
        tokio::time::sleep(tokio::time::Duration::from_millis(2000)).await;
    }
}

/// Stop a service by killing its process
pub async fn stop_service_process(info: &ServiceInfo) {
    if let Some(pid) = info.pid {
        kill_process_by_pid(pid).await;
        // Additional wait for file handles to release
        tokio::time::sleep(tokio::time::Duration::from_millis(500)).await;
    } else {
        // Fallback: kill by port
        kill_process_by_port(info.port).await;
    }
}

/// Setup log streaming for stdout/stderr
pub fn setup_log_streaming(
    stdout: Option<tokio::process::ChildStdout>,
    stderr: Option<tokio::process::ChildStderr>,
    app_handle: AppHandle,
    service_name: String,
    source: &str, // "build" or "run"
) {
    if let Some(stdout_stream) = stdout {
        let app_handle_stdout = app_handle.clone();
        let service_name_stdout = service_name.clone();
        let source_stdout = source.to_string();
        
        tokio::spawn(async move {
            let reader = TokioBufReader::new(stdout_stream);
            let mut lines = reader.lines();
            while let Ok(Some(line)) = lines.next_line().await {
                let _ = app_handle_stdout.emit_all(
                    "service-log",
                    serde_json::json!({
                        "service": service_name_stdout,
                        "type": "stdout",
                        "source": source_stdout,
                        "message": line,
                        "timestamp": std::time::SystemTime::now()
                            .duration_since(std::time::UNIX_EPOCH)
                            .unwrap()
                            .as_millis() as u64
                    }),
                );
            }
        });
    }

    if let Some(stderr_stream) = stderr {
        let app_handle_stderr = app_handle.clone();
        let service_name_stderr = service_name.clone();
        let source_stderr = source.to_string();
        
        tokio::spawn(async move {
            let reader = TokioBufReader::new(stderr_stream);
            let mut lines = reader.lines();
            while let Ok(Some(line)) = lines.next_line().await {
                let _ = app_handle_stderr.emit_all(
                    "service-log",
                    serde_json::json!({
                        "service": service_name_stderr,
                        "type": "stderr",
                        "source": source_stderr,
                        "message": line,
                        "timestamp": std::time::SystemTime::now()
                            .duration_since(std::time::UNIX_EPOCH)
                            .unwrap()
                            .as_millis() as u64
                    }),
                );
            }
        });
    }
}

/// Build a service project and stream output
pub async fn build_service(
    project_path: &str,
    service_name: &str,
    app_handle: AppHandle,
) -> Result<(), String> {
    let mut build_child = TokioCommand::new("dotnet")
        .args(&["build"])
        .current_dir(project_path)
        .stdout(Stdio::piped())
        .stderr(Stdio::piped())
        .spawn()
        .map_err(|e| format!("Failed to start build for {}: {}", service_name, e))?;

    let stdout = build_child.stdout.take();
    let stderr = build_child.stderr.take();
    
    setup_log_streaming(stdout, stderr, app_handle, service_name.to_string(), "build");

    let build_status = build_child.wait().await
        .map_err(|e| format!("Failed to wait for build: {}", e))?;

    if !build_status.success() {
        return Err(format!("Build failed for {}", service_name));
    }

    Ok(())
}

/// Check if a process is still running by PID
pub fn is_process_running(pid: u32) -> bool {
    #[cfg(target_os = "windows")]
    {
        Command::new("tasklist")
            .args(&["/FI", &format!("PID eq {}", pid)])
            .output()
            .map(|o| String::from_utf8_lossy(&o.stdout).contains(&pid.to_string()))
            .unwrap_or(false)
    }
    
    #[cfg(not(target_os = "windows"))]
    {
        std::process::Command::new("kill")
            .args(&["-0", &pid.to_string()])
            .output()
            .map(|o| o.status.success())
            .unwrap_or(false)
    }
}

