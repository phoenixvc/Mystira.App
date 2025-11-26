// Prevents additional console window on Windows in release, DO NOT REMOVE!!
#![cfg_attr(not(debug_assertions), windows_subsystem = "windows")]

use serde::{Deserialize, Serialize};
use serde_json::Value;
use std::process::{Command, Stdio};
use std::io::Write;
use std::path::PathBuf;
use std::env;
use std::sync::{Arc, Mutex};
use std::collections::HashMap;
use tauri::{State, Manager};
use tokio::process::Command as TokioCommand;
use tokio::io::{AsyncBufReadExt, BufReader as TokioBufReader};
use std::fs;

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

// Direct Azure CLI deployment commands
#[tauri::command]
async fn azure_deploy_infrastructure(
    repo_root: String,
    environment: String,
    resource_group: Option<String>,
    location: Option<String>,
    deploy_storage: Option<bool>,
    deploy_cosmos: Option<bool>,
    deploy_app_service: Option<bool>,
) -> Result<CommandResponse, String> {
    let env = environment.as_str();
    let rg = resource_group.unwrap_or_else(|| {
        match env {
            "dev" => "dev-euw-rg-mystira-app".to_string(),
            "prod" => "prod-euw-rg-mystira-app".to_string(),
            _ => format!("{}-euw-rg-mystira-app", env),
        }
    });
    let loc = location.unwrap_or_else(|| "westeurope".to_string());
    let sub_id = "22f9eb18-6553-4b7d-9451-47d0195085fe"; // Phoenix Azure Sponsorship
    
    let deployment_path = format!(
        "{}/src/Mystira.App.Infrastructure.Azure/Deployment/{}",
        repo_root, env
    );
    
    // Check if Azure CLI is available
    let az_check = Command::new("az")
        .arg("--version")
        .output();
    
    if az_check.is_err() {
        return Ok(CommandResponse {
            success: false,
            result: None,
            message: None,
            error: Some("Azure CLI not found. Please install Azure CLI first.".to_string()),
        });
    }
    
    // Check if logged in
    let account_check = Command::new("az")
        .arg("account")
        .arg("show")
        .output();
    
    if account_check.is_err() || !account_check.unwrap().status.success() {
        return Ok(CommandResponse {
            success: false,
            result: None,
            message: None,
            error: Some("Not logged in to Azure. Please run 'az login' first.".to_string()),
        });
    }
    
    // Set subscription
    let set_sub = Command::new("az")
        .arg("account")
        .arg("set")
        .arg("--subscription")
        .arg(sub_id)
        .output();
    
    if set_sub.is_err() || !set_sub.unwrap().status.success() {
        return Ok(CommandResponse {
            success: false,
            result: None,
            message: None,
            error: Some(format!("Failed to set subscription: {}", sub_id)),
        });
    }
    
    // Create resource group if it doesn't exist
    let rg_create = Command::new("az")
        .arg("group")
        .arg("create")
        .arg("--name")
        .arg(&rg)
        .arg("--location")
        .arg(&loc)
        .arg("--output")
        .arg("none")
        .output();
    
    // Ignore errors if resource group already exists
    
    // Deploy using bicep
    use std::time::{SystemTime, UNIX_EPOCH};
    let timestamp = SystemTime::now()
        .duration_since(UNIX_EPOCH)
        .unwrap()
        .as_secs();
    let deployment_name = format!("mystira-app-{}-{}", env, timestamp);
    
    // Use explicit deployment flags from frontend (more reliable than name matching)
    let deploy_storage = deploy_storage.unwrap_or(true);
    let deploy_cosmos = deploy_cosmos.unwrap_or(true);
    let deploy_app_service = deploy_app_service.unwrap_or(true);
    
    // Validate dependencies: App Service requires Cosmos and Storage
    if deploy_app_service && (!deploy_cosmos || !deploy_storage) {
        return Ok(CommandResponse {
            success: false,
            result: None,
            message: None,
            error: Some("App Service requires Cosmos DB and Storage Account to be deployed. Please select all dependencies.".to_string()),
        });
    }
    
    // Build parameters string
    let params = format!("environment={} location={} deployStorage={} deployCosmos={} deployAppService={}", 
        env, loc, deploy_storage, deploy_cosmos, deploy_app_service);
    
    // ⚠️ SAFETY: Always use Incremental mode to prevent accidental resource deletion
    // Incremental mode only creates/updates resources in the template, never deletes existing ones
    let deploy_output = Command::new("az")
        .arg("deployment")
        .arg("group")
        .arg("create")
        .arg("--resource-group")
        .arg(&rg)
        .arg("--template-file")
        .arg(format!("{}/main.bicep", deployment_path))
        .arg("--parameters")
        .arg(&params)
        .arg("--mode")
        .arg("Incremental")
        .arg("--name")
        .arg(&deployment_name)
        .current_dir(&deployment_path)
        .output();
    
    match deploy_output {
        Ok(output) => {
            if output.status.success() {
                // Get deployment outputs
                let outputs = Command::new("az")
                    .arg("deployment")
                    .arg("group")
                    .arg("show")
                    .arg("--resource-group")
                    .arg(&rg)
                    .arg("--name")
                    .arg(&deployment_name)
                    .arg("--query")
                    .arg("properties.outputs")
                    .arg("--output")
                    .arg("json")
                    .output();
                
                let outputs_json = outputs
                    .ok()
                    .and_then(|o| String::from_utf8(o.stdout).ok())
                    .and_then(|s| serde_json::from_str::<Value>(&s).ok());
                
                Ok(CommandResponse {
                    success: true,
                    result: Some(serde_json::json!({
                        "deploymentName": deployment_name,
                        "resourceGroup": rg,
                        "environment": env,
                        "outputs": outputs_json
                    })),
                    message: Some(format!("Infrastructure deployed successfully to {}", rg)),
                    error: None,
                })
            } else {
                let error_msg = String::from_utf8_lossy(&output.stderr);
                Ok(CommandResponse {
                    success: false,
                    result: None,
                    message: None,
                    error: Some(format!("Deployment failed: {}", error_msg)),
                })
            }
        }
        Err(e) => Ok(CommandResponse {
            success: false,
            result: None,
            message: None,
            error: Some(format!("Failed to execute deployment: {}", e)),
        }),
    }
}

#[tauri::command]
async fn check_infrastructure_exists(
    environment: String,
    resource_group: Option<String>,
) -> Result<CommandResponse, String> {
    let rg = resource_group.unwrap_or_else(|| format!("{}-euw-rg-mystira-app", environment));
    
    // Check if resource group exists and has resources
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
            
            // Check for key resources (App Service, Cosmos DB, Storage)
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
                            r.get("type").and_then(|t| t.as_str())
                                .map(|t| t.contains("Microsoft.Web/sites"))
                                .unwrap_or(false)
                        });
                        let has_cosmos = resources.iter().any(|r| {
                            r.get("type").and_then(|t| t.as_str())
                                .map(|t| t.contains("Microsoft.DocumentDB"))
                                .unwrap_or(false)
                        });
                        let has_storage = resources.iter().any(|r| {
                            r.get("type").and_then(|t| t.as_str())
                                .map(|t| t.contains("Microsoft.Storage"))
                                .unwrap_or(false)
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

#[tauri::command]
async fn azure_validate_infrastructure(
    repo_root: String,
    environment: String,
    resource_group: Option<String>,
) -> Result<CommandResponse, String> {
    let env = environment.as_str();
    let rg = resource_group.unwrap_or_else(|| {
        match env {
            "dev" => "dev-euw-rg-mystira-app".to_string(),
            "prod" => "prod-euw-rg-mystira-app".to_string(),
            _ => format!("{}-euw-rg-mystira-app", env),
        }
    });
    let sub_id = "22f9eb18-6553-4b7d-9451-47d0195085fe";
    
    let deployment_path = format!(
        "{}/src/Mystira.App.Infrastructure.Azure/Deployment/{}",
        repo_root, env
    );
    
    // Set subscription
    let _ = Command::new("az")
        .arg("account")
        .arg("set")
        .arg("--subscription")
        .arg(sub_id)
        .output();
    
    // Validate bicep
    let validate_output = Command::new("az")
        .arg("deployment")
        .arg("group")
        .arg("validate")
        .arg("--resource-group")
        .arg(&rg)
        .arg("--template-file")
        .arg(format!("{}/main.bicep", deployment_path))
        .arg("--parameters")
        .arg(format!("environment={} location=westeurope", env))
        .current_dir(&deployment_path)
        .output();
    
    match validate_output {
        Ok(output) => {
            if output.status.success() {
                Ok(CommandResponse {
                    success: true,
                    result: Some(serde_json::json!({
                        "message": "Bicep templates are valid"
                    })),
                    message: Some("Validation successful".to_string()),
                    error: None,
                })
            } else {
                let error_msg = String::from_utf8_lossy(&output.stderr);
                Ok(CommandResponse {
                    success: false,
                    result: None,
                    message: None,
                    error: Some(format!("Validation failed: {}", error_msg)),
                })
            }
        }
        Err(e) => Ok(CommandResponse {
            success: false,
            result: None,
            message: None,
            error: Some(format!("Failed to validate: {}", e)),
        }),
    }
}

#[tauri::command]
async fn azure_preview_infrastructure(
    repo_root: String,
    environment: String,
    resource_group: Option<String>,
) -> Result<CommandResponse, String> {
    let env = environment.as_str();
    let rg = resource_group.unwrap_or_else(|| {
        match env {
            "dev" => "dev-euw-rg-mystira-app".to_string(),
            "prod" => "prod-euw-rg-mystira-app".to_string(),
            _ => format!("{}-euw-rg-mystira-app", env),
        }
    });
    let sub_id = "22f9eb18-6553-4b7d-9451-47d0195085fe";
    
    let deployment_path = format!(
        "{}/src/Mystira.App.Infrastructure.Azure/Deployment/{}",
        repo_root, env
    );
    
    // Set subscription
    let _ = Command::new("az")
        .arg("account")
        .arg("set")
        .arg("--subscription")
        .arg(sub_id)
        .output();
    
    // Create resource group if it doesn't exist (needed for what-if)
    let _ = Command::new("az")
        .arg("group")
        .arg("create")
        .arg("--name")
        .arg(&rg)
        .arg("--location")
        .arg("westeurope")
        .arg("--output")
        .arg("none")
        .output();
    
    // Preview changes (what-if) - deploy all for preview
    let preview_output = Command::new("az")
        .arg("deployment")
        .arg("group")
        .arg("what-if")
        .arg("--resource-group")
        .arg(&rg)
        .arg("--template-file")
        .arg(format!("{}/main.bicep", deployment_path))
        .arg("--parameters")
        .arg(format!("environment={} location=westeurope deployStorage=true deployCosmos=true deployAppService=true", env))
        .arg("--output")
        .arg("json")
        .current_dir(&deployment_path)
        .output();
    
    match preview_output {
        Ok(output) => {
            let stdout = String::from_utf8_lossy(&output.stdout);
            let stderr = String::from_utf8_lossy(&output.stderr);
            
            // Try to parse JSON output
            let parsed_json: Option<Value> = serde_json::from_str(&stdout).ok();
            
            Ok(CommandResponse {
                success: output.status.success(),
                result: Some(serde_json::json!({
                    "preview": stdout.to_string(),
                    "parsed": parsed_json,
                    "errors": if !output.status.success() { Some(stderr.to_string()) } else { None }
                })),
                message: if output.status.success() {
                    Some("Preview generated successfully".to_string())
                } else {
                    None
                },
                error: if !output.status.success() {
                    Some(stderr.to_string())
                } else {
                    None
                },
            })
        }
        Err(e) => Ok(CommandResponse {
            success: false,
            result: None,
            message: None,
            error: Some(format!("Failed to preview: {}", e)),
        }),
    }
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
async fn prebuild_service(
    service_name: String,
    repo_root: String,
    app_handle: tauri::AppHandle,
    services: State<'_, ServiceManager>,
) -> Result<(), String> {
    // Validate repo_root is not empty
    if repo_root.is_empty() {
        return Err(format!("Repository root is empty. Please configure the repository root in DevHub."));
    }
    
    // Convert to PathBuf for proper path handling
    let repo_path = PathBuf::from(&repo_root);
    if !repo_path.exists() {
        return Err(format!("Repository root does not exist: {}", repo_root));
    }
    
    // Stop ALL services before building to avoid file locks on shared DLLs (like Domain.dll)
    // All services share Domain.dll, so we need to stop all of them before building any
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
    for (svc_name, pid_opt, port) in services_to_stop {
        // Remove from services map first
        {
            let mut services_guard = services.lock().map_err(|e| format!("Lock error: {}", e))?;
            services_guard.remove(&svc_name);
        }
        
        // Kill the process
        #[cfg(target_os = "windows")]
        {
            if let Some(pid_val) = pid_opt {
                let _ = Command::new("taskkill")
                    .args(&["/F", "/PID", &pid_val.to_string()])
                    .output();
                
                // Wait for process to terminate (up to 5 seconds)
                for _ in 0..50 {
                    let check = Command::new("tasklist")
                        .args(&["/FI", &format!("PID eq {}", pid_val)])
                        .output();
                    
                    if let Ok(output) = check {
                        let output_str = String::from_utf8_lossy(&output.stdout);
                        if !output_str.contains(&pid_val.to_string()) {
                            break;
                        }
                    }
                    tokio::time::sleep(tokio::time::Duration::from_millis(100)).await;
                }
            } else {
                // Fallback: kill by port
                let _ = Command::new("powershell")
                    .args(&[
                        "-Command",
                        &format!("Get-NetTCPConnection -LocalPort {} -ErrorAction SilentlyContinue | Select-Object -ExpandProperty OwningProcess | ForEach-Object {{ Stop-Process -Id $_ -Force }}", port)
                    ])
                    .output();
                tokio::time::sleep(tokio::time::Duration::from_millis(2000)).await;
            }
        }
        
        #[cfg(not(target_os = "windows"))]
        {
            if let Some(pid_val) = pid_opt {
                let _ = Command::new("kill")
                    .args(&["-9", &pid_val.to_string()])
                    .output();
            }
        }
    }
    
    // Wait longer for all file handles to release (especially important for shared DLLs)
    if !services_to_stop.is_empty() {
        tokio::time::sleep(tokio::time::Duration::from_millis(3000)).await;
    }
    
    let (project_path, _port, _url) = match service_name.as_str() {
        "api" => (
            repo_path.join("src").join("Mystira.App.Api"),
            7096,
            Some("https://localhost:7096/swagger".to_string()),
        ),
        "admin-api" => (
            repo_path.join("src").join("Mystira.App.Admin.Api"),
            7097,
            Some("https://localhost:7097/swagger".to_string()),
        ),
        "pwa" => (
            repo_path.join("src").join("Mystira.App.PWA"),
            7000,
            Some("http://localhost:7000".to_string()),
        ),
        _ => return Err(format!("Unknown service: {}", service_name)),
    };
    
    // Validate project path exists
    if !project_path.exists() {
        return Err(format!("Project directory does not exist: {}", project_path.display()));
    }
    
    // Convert PathBuf to string for current_dir
    let project_path_str = project_path.to_string_lossy().to_string();
    
    // Additional wait for any remaining file handles to release
    tokio::time::sleep(tokio::time::Duration::from_millis(1000)).await;

    // Build with streaming output
    let mut build_child = TokioCommand::new("dotnet")
        .args(&["build"])
        .current_dir(&project_path_str)
        .stdout(Stdio::piped())
        .stderr(Stdio::piped())
        .spawn()
        .map_err(|e| format!("Failed to start build for {}: {}", service_name, e))?;

    // Stream build output as logs
    let app_handle_build_stdout = app_handle.clone();
    let app_handle_build_stderr = app_handle.clone();
    let service_name_build_stdout = service_name.clone();
    let service_name_build_stderr = service_name.clone();

    if let Some(build_stdout) = build_child.stdout.take() {
        tokio::spawn(async move {
            let reader = TokioBufReader::new(build_stdout);
            let mut lines = reader.lines();
            while let Ok(Some(line)) = lines.next_line().await {
                let _ = app_handle_build_stdout.emit_all(
                    "service-log",
                    serde_json::json!({
                        "service": service_name_build_stdout,
                        "type": "stdout",
                        "source": "build",
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

    if let Some(build_stderr) = build_child.stderr.take() {
        tokio::spawn(async move {
            let reader = TokioBufReader::new(build_stderr);
            let mut lines = reader.lines();
            while let Ok(Some(line)) = lines.next_line().await {
                let _ = app_handle_build_stderr.emit_all(
                    "service-log",
                    serde_json::json!({
                        "service": service_name_build_stderr,
                        "type": "stderr",
                        "source": "build",
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

    // Wait for build to complete
    let build_status = build_child.wait().await
        .map_err(|e| format!("Failed to wait for build: {}", e))?;

    if !build_status.success() {
        return Err(format!("Build failed for {}", service_name));
    }

    Ok(())
}

#[tauri::command]
async fn start_service(
    service_name: String,
    repo_root: String,
    services: State<'_, ServiceManager>,
    app_handle: tauri::AppHandle,
) -> Result<ServiceStatus, String> {
    // Check if service is already running (drop guard immediately)
    {
        let services_guard = services.lock().map_err(|e| format!("Lock error: {}", e))?;
        if services_guard.contains_key(&service_name) {
            return Err(format!("Service {} is already running", service_name));
        }
    } // Guard is dropped here

    // Validate repo_root is not empty
    if repo_root.is_empty() {
        return Err(format!("Repository root is empty. Please configure the repository root in DevHub."));
    }
    
    // Convert to PathBuf for proper path handling
    let repo_path = PathBuf::from(&repo_root);
    if !repo_path.exists() {
        return Err(format!("Repository root does not exist: {}", repo_root));
    }
    
    let (project_path, port, url) = match service_name.as_str() {
        "api" => (
            repo_path.join("src").join("Mystira.App.Api"),
            7096,
            Some("https://localhost:7096/swagger".to_string()),
        ),
        "admin-api" => (
            repo_path.join("src").join("Mystira.App.Admin.Api"),
            7097,
            Some("https://localhost:7097/swagger".to_string()),
        ),
        "pwa" => (
            repo_path.join("src").join("Mystira.App.PWA"),
            7000,
            Some("http://localhost:7000".to_string()),
        ),
        _ => return Err(format!("Unknown service: {}", service_name)),
    };
    
    // Validate project path exists
    if !project_path.exists() {
        return Err(format!("Project directory does not exist: {}", project_path.display()));
    }
    
    // Convert PathBuf to string for current_dir
    let project_path_str = project_path.to_string_lossy().to_string();

    // Build with streaming output
    let mut build_child = TokioCommand::new("dotnet")
        .args(&["build"])
        .current_dir(&project_path_str)
        .stdout(Stdio::piped())
        .stderr(Stdio::piped())
        .spawn()
        .map_err(|e| format!("Failed to start build for {}: {}", service_name, e))?;

    // Stream build output as logs
    let app_handle_build_stdout = app_handle.clone();
    let app_handle_build_stderr = app_handle.clone();
    let service_name_build_stdout = service_name.clone();
    let service_name_build_stderr = service_name.clone();

    if let Some(build_stdout) = build_child.stdout.take() {
        tokio::spawn(async move {
            let reader = TokioBufReader::new(build_stdout);
            let mut lines = reader.lines();
            while let Ok(Some(line)) = lines.next_line().await {
                let _ = app_handle_build_stdout.emit_all(
                    "service-log",
                    serde_json::json!({
                        "service": service_name_build_stdout,
                        "type": "stdout",
                        "source": "build",
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

    if let Some(build_stderr) = build_child.stderr.take() {
        tokio::spawn(async move {
            let reader = TokioBufReader::new(build_stderr);
            let mut lines = reader.lines();
            while let Ok(Some(line)) = lines.next_line().await {
                let _ = app_handle_build_stderr.emit_all(
                    "service-log",
                    serde_json::json!({
                        "service": service_name_build_stderr,
                        "type": "stderr",
                        "source": "build",
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

    // Wait for build to complete
    let build_status = build_child.wait().await
        .map_err(|e| format!("Failed to wait for build: {}", e))?;

    if !build_status.success() {
        return Err(format!("Build failed for {}", service_name));
    }

    // Start the service with tokio for async stdout/stderr reading
    let mut child = TokioCommand::new("dotnet")
        .arg("run")
        .current_dir(&project_path_str)
        .stdout(Stdio::piped())
        .stderr(Stdio::piped())
        .spawn()
        .map_err(|e| format!("Failed to start {}: {} (path: {})", service_name, e, project_path_str))?;

    // Get the process ID
    let pid = child.id();

    // Take stdout and stderr BEFORE moving child into spawn
    let stdout = child.stdout.take();
    let stderr = child.stderr.take();

    // Store service info (re-acquire lock after build)
    let service_info = ServiceInfo {
        name: service_name.clone(),
        port,
        url: url.clone(),
        pid,
    };
    {
        let mut services_guard = services.lock().map_err(|e| format!("Lock error: {}", e))?;
        services_guard.insert(service_name.clone(), service_info.clone());
    } // Guard dropped here
    
    // Clone the Arc from the State before spawning
    let services_arc = Arc::clone(&*services);
    let service_name_clone = service_name.clone();
    
    // Spawn a task to wait for the process (keeps it alive)
    // When it exits, remove it from the services map
    tokio::spawn(async move {
        let _ = child.wait().await;
        // Process exited, remove from services
        if let Ok(mut guard) = services_arc.lock() {
            guard.remove(&service_name_clone);
        }
    });

    // Spawn tasks to read stdout and stderr and emit events
    let app_handle_stdout = app_handle.clone();
    let app_handle_stderr = app_handle.clone();
    let service_name_stdout = service_name.clone();
    let service_name_stderr = service_name.clone();

    if let Some(stdout) = stdout {
        tokio::spawn(async move {
            let reader = TokioBufReader::new(stdout);
            let mut lines = reader.lines();
            while let Ok(Some(line)) = lines.next_line().await {
                let _ = app_handle_stdout.emit_all(
                    "service-log",
                    serde_json::json!({
                        "service": service_name_stdout,
                        "type": "stdout",
                        "source": "run",
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

    if let Some(stderr) = stderr {
        tokio::spawn(async move {
            let reader = TokioBufReader::new(stderr);
            let mut lines = reader.lines();
            while let Ok(Some(line)) = lines.next_line().await {
                let _ = app_handle_stderr.emit_all(
                    "service-log",
                    serde_json::json!({
                        "service": service_name_stderr,
                        "type": "stderr",
                        "source": "run",
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
    let pid;
    let port;
    
    // Extract PID and port while holding the lock, then drop it
    {
        let mut services_guard = services.lock().map_err(|e| format!("Lock error: {}", e))?;
        
        if let Some(info) = services_guard.remove(&service_name) {
            pid = info.pid;
            port = info.port;
        } else {
            return Err(format!("Service {} is not running", service_name));
        }
    }
    
    // Now we can await without holding the lock
    #[cfg(target_os = "windows")]
    {
        if let Some(pid_val) = pid {
            // Try to kill by PID
            let _ = Command::new("taskkill")
                .args(&["/F", "/PID", &pid_val.to_string()])
                .output();
            
            // Wait for process to actually terminate (up to 3 seconds)
            for _ in 0..30 {
                let check = Command::new("tasklist")
                    .args(&["/FI", &format!("PID eq {}", pid_val)])
                    .output();
                
                if let Ok(output) = check {
                    let output_str = String::from_utf8_lossy(&output.stdout);
                    if !output_str.contains(&pid_val.to_string()) {
                        // Process is gone, wait a bit more for file handles to release
                        tokio::time::sleep(tokio::time::Duration::from_millis(500)).await;
                        break;
                    }
                }
                tokio::time::sleep(tokio::time::Duration::from_millis(100)).await;
            }
        } else {
            // Fallback: kill by port
            let _ = Command::new("powershell")
                .args(&[
                    "-Command",
                    &format!("Get-NetTCPConnection -LocalPort {} -ErrorAction SilentlyContinue | Select-Object -ExpandProperty OwningProcess | ForEach-Object {{ Stop-Process -Id $_ -Force }}", port)
                ])
                .output();
            // Wait a bit for file handles to release
            tokio::time::sleep(tokio::time::Duration::from_millis(2000)).await;
        }
    }
    
    #[cfg(not(target_os = "windows"))]
    {
        if let Some(pid_val) = pid {
            let _ = Command::new("kill")
                .args(&["-9", &pid_val.to_string()])
                .output();
            // Wait a bit for file handles to release
            tokio::time::sleep(tokio::time::Duration::from_millis(1000)).await;
        }
    }
    
    Ok(())
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
async fn get_service_port(service_name: String, repo_root: String) -> Result<u16, String> {
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

#[tauri::command]
async fn update_service_port(service_name: String, repo_root: String, new_port: u16) -> Result<(), String> {
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

#[tauri::command]
async fn find_available_port(start_port: u16) -> Result<u16, String> {
    // Try ports starting from start_port, up to start_port + 100
    for port in start_port..(start_port + 100) {
        let available = check_port_available(port).await?;
        if available {
            return Ok(port);
        }
    }
    Err("Could not find available port".to_string())
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
    let services: ServiceManager = Arc::new(Mutex::new(HashMap::new()));
    
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
            azure_deploy_infrastructure,
            azure_validate_infrastructure,
            azure_preview_infrastructure,
            check_infrastructure_exists,
            get_azure_resources,
            get_github_deployments,
            test_connection,
            prebuild_service,
            start_service,
            stop_service,
            get_service_status,
            get_repo_root,
            get_current_branch,
            create_webview_window,
            check_port_available,
            check_service_health,
            get_service_port,
            update_service_port,
            find_available_port,
        ])
        .run(tauri::generate_context!())
        .expect("error while running tauri application");
}
