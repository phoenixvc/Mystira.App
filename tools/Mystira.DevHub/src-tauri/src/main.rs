// Prevents additional console window on Windows in release, DO NOT REMOVE!!
#![cfg_attr(not(debug_assertions), windows_subsystem = "windows")]

use serde::{Deserialize, Serialize};
use std::process::{Command, Stdio, Child};
use std::io::{Write, BufRead, BufReader};
use std::path::PathBuf;
use std::env;
use std::sync::{Arc, Mutex};
use std::collections::HashMap;
use tauri::{State, Manager, Emitter};
use tokio::process::Command as TokioCommand;
use tokio::io::{AsyncBufReadExt, BufReader as TokioBufReader};
use tokio::sync::mpsc;

#[derive(Debug, Serialize, Deserialize)]
struct CommandRequest {
    command: String,
    args: serde_json::Value,
}

#[derive(Debug, Serialize, Deserialize)]
struct CommandResponse {
    success: bool,
    result: Option<serde_json::Value>,
    message: Option<String>,
    error: Option<String>,
}

#[derive(Debug, Serialize, Deserialize, Clone)]
struct ServiceStatus {
    name: String,
    running: bool,
    port: Option<u16>,
    url: Option<String>,
}

// Service info with process and log channel
#[derive(Clone)]
struct ServiceInfo {
    name: String,
    port: u16,
    url: Option<String>,
    pid: Option<u32>, // Store process ID for killing
}

// Global service manager - store service info
type ServiceManager = Arc<Mutex<HashMap<String, ServiceInfo>>>;

// Get the path to the built .NET CLI executable
fn get_cli_executable_path() -> Result<PathBuf, String> {
    // Try to find the executable relative to the Tauri app
    let base_path = env::current_dir()
        .map_err(|e| format!("Failed to get current directory: {}", e))?;

    // Check for pre-built executable in multiple locations
    let possible_paths = vec![
        // Development: relative to Tauri project
        base_path.join("../../Mystira.DevHub.CLI/bin/Debug/net9.0/Mystira.DevHub.CLI"),
        base_path.join("../../Mystira.DevHub.CLI/bin/Release/net9.0/Mystira.DevHub.CLI"),
        // Production: bundled with app
        base_path.join("Mystira.DevHub.CLI"),
        base_path.join("bin/Mystira.DevHub.CLI"),
    ];

    // Add .exe extension on Windows
    #[cfg(target_os = "windows")]
    let possible_paths: Vec<PathBuf> = possible_paths
        .iter()
        .map(|p| p.with_extension("exe"))
        .collect();

    // Find the first path that exists
    for path in &possible_paths {
        if path.exists() {
            return Ok(path.clone());
        }
    }

    // If no built executable found, provide helpful error
    Err(format!(
        "Could not find Mystira.DevHub.CLI executable. Please build it first:\n\
         cd tools/Mystira.DevHub.CLI\n\
         dotnet build\n\
         \n\
         Searched in:\n{}",
        possible_paths
            .iter()
            .map(|p| format!("  - {}", p.display()))
            .collect::<Vec<_>>()
            .join("\n")
    ))
}

// Execute .NET CLI wrapper and return response
async fn execute_devhub_cli(command: String, args: serde_json::Value) -> Result<CommandResponse, String> {
    let request = CommandRequest {
        command: command.clone(),
        args,
    };

    let request_json = serde_json::to_string(&request)
        .map_err(|e| format!("Failed to serialize request: {}", e))?;

    // Get the CLI executable path
    let cli_exe_path = get_cli_executable_path()?;

    // Spawn the .NET process
    let mut child = Command::new(&cli_exe_path)
        .stdin(Stdio::piped())
        .stdout(Stdio::piped())
        .stderr(Stdio::piped())
        .spawn()
        .map_err(|e| format!("Failed to spawn process at {}: {}", cli_exe_path.display(), e))?;

    // Write JSON to stdin
    if let Some(mut stdin) = child.stdin.take() {
        stdin.write_all(request_json.as_bytes())
            .map_err(|e| format!("Failed to write to stdin: {}", e))?;
        // Close stdin to signal we're done writing
        drop(stdin);
    }

    // Wait for the process to complete and read output
    let output = child.wait_with_output()
        .map_err(|e| format!("Failed to wait for process: {}", e))?;

    if !output.status.success() {
        let stderr = String::from_utf8_lossy(&output.stderr);
        let stdout = String::from_utf8_lossy(&output.stdout);
        return Err(format!(
            "Process failed with exit code: {:?}\nStderr: {}\nStdout: {}",
            output.status.code(),
            stderr,
            stdout
        ));
    }

    // Parse the JSON response
    let stdout = String::from_utf8_lossy(&output.stdout);

    // Handle empty response
    if stdout.trim().is_empty() {
        return Err("Process returned empty response".to_string());
    }

    let response: CommandResponse = serde_json::from_str(&stdout)
        .map_err(|e| format!("Failed to parse response JSON: {}. Output was: {}", e, stdout))?;

    Ok(response)
}

// Cosmos Commands
#[tauri::command]
async fn cosmos_export(output_path: String) -> Result<CommandResponse, String> {
    let args = serde_json::json!({
        "outputPath": output_path
    });
    execute_devhub_cli("cosmos.export".to_string(), args).await
}

#[tauri::command]
async fn cosmos_stats() -> Result<CommandResponse, String> {
    execute_devhub_cli("cosmos.stats".to_string(), serde_json::json!({})).await
}

// Migration Commands
#[tauri::command]
async fn migration_run(
    migration_type: String,
    source_cosmos: Option<String>,
    dest_cosmos: Option<String>,
    source_storage: Option<String>,
    dest_storage: Option<String>,
    database_name: String,
    container_name: String,
) -> Result<CommandResponse, String> {
    let args = serde_json::json!({
        "type": migration_type,
        "sourceCosmosConnection": source_cosmos,
        "destCosmosConnection": dest_cosmos,
        "sourceStorageConnection": source_storage,
        "destStorageConnection": dest_storage,
        "databaseName": database_name,
        "containerName": container_name
    });
    execute_devhub_cli("migration.run".to_string(), args).await
}

// Infrastructure Commands
#[tauri::command]
async fn infrastructure_validate(workflow_file: String, repository: String) -> Result<CommandResponse, String> {
    let args = serde_json::json!({
        "workflowFile": workflow_file,
        "repository": repository
    });
    execute_devhub_cli("infrastructure.validate".to_string(), args).await
}

#[tauri::command]
async fn infrastructure_preview(workflow_file: String, repository: String) -> Result<CommandResponse, String> {
    let args = serde_json::json!({
        "workflowFile": workflow_file,
        "repository": repository
    });
    execute_devhub_cli("infrastructure.preview".to_string(), args).await
}

#[tauri::command]
async fn infrastructure_deploy(workflow_file: String, repository: String) -> Result<CommandResponse, String> {
    let args = serde_json::json!({
        "workflowFile": workflow_file,
        "repository": repository
    });
    execute_devhub_cli("infrastructure.deploy".to_string(), args).await
}

#[tauri::command]
async fn infrastructure_destroy(workflow_file: String, repository: String, confirm: bool) -> Result<CommandResponse, String> {
    let args = serde_json::json!({
        "workflowFile": workflow_file,
        "repository": repository,
        "confirm": confirm
    });
    execute_devhub_cli("infrastructure.destroy".to_string(), args).await
}

#[tauri::command]
async fn infrastructure_status(workflow_file: String, repository: String) -> Result<CommandResponse, String> {
    let args = serde_json::json!({
        "workflowFile": workflow_file,
        "repository": repository
    });
    execute_devhub_cli("infrastructure.status".to_string(), args).await
}

// New commands for Wave 1: Real integrations

#[tauri::command]
async fn get_azure_resources(subscription_id: Option<String>) -> Result<CommandResponse, String> {
    let args = if let Some(sub_id) = subscription_id {
        serde_json::json!({ "subscriptionId": sub_id })
    } else {
        serde_json::json!({})
    };
    execute_devhub_cli("azure.list-resources".to_string(), args).await
}

#[tauri::command]
async fn get_github_deployments(repository: String, limit: Option<i32>) -> Result<CommandResponse, String> {
    let args = serde_json::json!({
        "repository": repository,
        "limit": limit.unwrap_or(10)
    });
    execute_devhub_cli("github.list-deployments".to_string(), args).await
}

#[tauri::command]
async fn test_connection(connection_type: String, connection_string: Option<String>) -> Result<CommandResponse, String> {
    let args = serde_json::json!({
        "type": connection_type,
        "connectionString": connection_string
    });
    execute_devhub_cli("connection.test".to_string(), args).await
}

// Service Management Commands
#[tauri::command]
async fn start_service(
    service_name: String,
    repo_root: String,
    services: State<'_, ServiceManager>,
    app_handle: tauri::AppHandle,
) -> Result<ServiceStatus, String> {
    let mut services_guard = services.lock().map_err(|e| format!("Lock error: {}", e))?;
    
    // Check if service is already running
    if services_guard.contains_key(&service_name) {
        return Err(format!("Service {} is already running", service_name));
    }

    let (project_path, port, url) = match service_name.as_str() {
        "api" => (
            format!("{}\\src\\Mystira.App.Api", repo_root),
            7096,
            Some("https://localhost:7096/swagger".to_string()),
        ),
        "admin-api" => (
            format!("{}\\src\\Mystira.App.Admin.Api", repo_root),
            7096,
            Some("https://localhost:7096/admin".to_string()),
        ),
        "pwa" => (
            format!("{}\\src\\Mystira.App.PWA", repo_root),
            7000,
            Some("http://localhost:7000".to_string()),
        ),
        _ => return Err(format!("Unknown service: {}", service_name)),
    };

    // Build first
    let build_output = Command::new("dotnet")
        .args(&["build"])
        .current_dir(&project_path)
        .output()
        .map_err(|e| format!("Failed to build {}: {}", service_name, e))?;

    if !build_output.status.success() {
        let stderr = String::from_utf8_lossy(&build_output.stderr);
        return Err(format!("Build failed for {}: {}", service_name, stderr));
    }

    // Start the service with tokio for async stdout/stderr reading
    let mut child = TokioCommand::new("dotnet")
        .arg("run")
        .current_dir(&project_path)
        .stdout(Stdio::piped())
        .stderr(Stdio::piped())
        .spawn()
        .map_err(|e| format!("Failed to start {}: {}", service_name, e))?;

    // Get the process ID
    let pid = child.id();

    // Store service info
    let service_info = ServiceInfo {
        name: service_name.clone(),
        port,
        url: url.clone(),
        pid: Some(pid),
    };
    services_guard.insert(service_name.clone(), service_info.clone());
    
    // Spawn a task to wait for the process (keeps it alive)
    // When it exits, remove it from the services map
    let services_clone = services.clone();
    let service_name_clone = service_name.clone();
    tokio::spawn(async move {
        let _ = child.wait().await;
        // Process exited, remove from services
        if let Ok(mut guard) = services_clone.lock() {
            guard.remove(&service_name_clone);
        }
    });

    // Spawn tasks to read stdout and stderr and emit events
    let app_handle_stdout = app_handle.clone();
    let app_handle_stderr = app_handle.clone();
    let service_name_stdout = service_name.clone();
    let service_name_stderr = service_name.clone();

    if let Some(stdout) = child.stdout.take() {
        tokio::spawn(async move {
            let reader = TokioBufReader::new(stdout);
            let mut lines = reader.lines();
            while let Ok(Some(line)) = lines.next_line().await {
                let _ = app_handle_stdout.emit_all(
                    "service-log",
                    serde_json::json!({
                        "service": service_name_stdout,
                        "type": "stdout",
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

    if let Some(stderr) = child.stderr.take() {
        tokio::spawn(async move {
            let reader = TokioBufReader::new(stderr);
            let mut lines = reader.lines();
            while let Ok(Some(line)) = lines.next_line().await {
                let _ = app_handle_stderr.emit_all(
                    "service-log",
                    serde_json::json!({
                        "service": service_name_stderr,
                        "type": "stderr",
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

    // Store the child process handle (we'll need to track it differently)
    // For now, we'll just let it run and track by name

    Ok(ServiceStatus {
        name: service_name,
        running: true,
        port: Some(port),
        url,
    })
}

#[tauri::command]
async fn stop_service(
    service_name: String,
    services: State<'_, ServiceManager>,
) -> Result<(), String> {
    let mut services_guard = services.lock().map_err(|e| format!("Lock error: {}", e))?;
    
    if let Some(info) = services_guard.remove(&service_name) {
        // Try to kill the process by PID first, then by port as fallback
        #[cfg(target_os = "windows")]
        {
            if let Some(pid) = info.pid {
                // Try to kill by PID
                let _ = Command::new("taskkill")
                    .args(&["/F", "/PID", &pid.to_string()])
                    .output();
            } else {
                // Fallback: kill by port
                let port = info.port;
                let _ = Command::new("powershell")
                    .args(&[
                        "-Command",
                        &format!("Get-NetTCPConnection -LocalPort {} -ErrorAction SilentlyContinue | Select-Object -ExpandProperty OwningProcess | ForEach-Object {{ Stop-Process -Id $_ -Force }}", port)
                    ])
                    .output();
            }
        }
        
        #[cfg(not(target_os = "windows"))]
        {
            if let Some(pid) = info.pid {
                let _ = Command::new("kill")
                    .args(&["-9", &pid.to_string()])
                    .output();
            }
        }
        
        Ok(())
    } else {
        Err(format!("Service {} is not running", service_name))
    }
}

#[tauri::command]
async fn get_service_status(
    services: State<'_, ServiceManager>,
) -> Result<Vec<ServiceStatus>, String> {
    let services_guard = services.lock().map_err(|e| format!("Lock error: {}", e))?;
    
    let mut statuses = Vec::new();
    for (_name, info) in services_guard.iter() {
        // Check if process is still running by PID
        let is_running = if let Some(pid) = info.pid {
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
                // On Unix, check if process exists
                std::process::Command::new("kill")
                    .args(&["-0", &pid.to_string()])
                    .output()
                    .map(|o| o.status.success())
                    .unwrap_or(false)
            }
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

#[tauri::command]
async fn check_port_available(port: u16) -> Result<bool, String> {
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

#[tauri::command]
async fn check_service_health(url: String) -> Result<bool, String> {
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

#[tauri::command]
async fn get_repo_root() -> Result<String, String> {
    // Get the current working directory (where DevHub is running from)
    let current_dir = env::current_dir()
        .map_err(|e| format!("Failed to get current directory: {}", e))?;
    
    // Check if current directory is the repo root (has .git or solution file)
    if current_dir.join(".git").exists() || current_dir.join("Mystira.App.sln").exists() {
        return Ok(current_dir.to_string_lossy().to_string());
    }
    
    // If we're in tools/Mystira.DevHub, go up two levels
    if current_dir.ends_with("tools/Mystira.DevHub") || current_dir.ends_with("tools\\Mystira.DevHub") {
        if let Some(repo_root) = current_dir.parent()
            .and_then(|p| p.parent()) {
            if repo_root.join(".git").exists() || repo_root.join("Mystira.App.sln").exists() {
                return Ok(repo_root.to_string_lossy().to_string());
            }
        }
    }
    
    // Fallback: try to find the repo root by walking up the directory tree
    let mut dir = current_dir.clone();
    loop {
        if dir.join(".git").exists() || dir.join("Mystira.App.sln").exists() {
            return Ok(dir.to_string_lossy().to_string());
        }
        match dir.parent() {
            Some(parent) => dir = parent.to_path_buf(),
            None => {
                // If we can't find the repo root, return the current directory as fallback
                // This allows users to set it manually
                return Ok(current_dir.to_string_lossy().to_string());
            }
        }
    }
}

#[tauri::command]
async fn get_current_branch(repo_root: String) -> Result<String, String> {
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

#[tauri::command]
async fn create_webview_window(
    url: String,
    title: String,
    app_handle: tauri::AppHandle,
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

fn main() {
    // Initialize service manager
    let services: Arc<Mutex<HashMap<String, Child>>> = Arc::new(Mutex::new(HashMap::new()));
    
    tauri::Builder::default()
        .manage(services)
        .invoke_handler(tauri::generate_handler![
            cosmos_export,
            cosmos_stats,
            migration_run,
            infrastructure_validate,
            infrastructure_preview,
            infrastructure_deploy,
            infrastructure_destroy,
            infrastructure_status,
            get_azure_resources,
            get_github_deployments,
            test_connection,
            start_service,
            stop_service,
            get_service_status,
            get_repo_root,
            get_current_branch,
            create_webview_window,
            check_port_available,
            check_service_health,
        ])
        .run(tauri::generate_context!())
        .expect("error while running tauri application");
}
