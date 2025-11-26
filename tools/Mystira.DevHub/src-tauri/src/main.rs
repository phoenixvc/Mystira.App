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

// Helper function to get current Azure subscription ID from CLI
fn get_azure_subscription_id() -> Result<String, String> {
    let program_files = env::var("ProgramFiles").unwrap_or_else(|_| "C:\\Program Files".to_string());
    let az_path = format!("{}\\Microsoft SDKs\\Azure\\CLI2\\wbin\\az.cmd", program_files);
    let az_path_buf = PathBuf::from(&az_path);
    let use_direct_path = az_path_buf.exists();
    
    let output = if use_direct_path {
        Command::new("powershell")
            .arg("-NoProfile")
            .arg("-Command")
            .arg(format!("& '{}' account show --query id --output tsv", az_path.replace("'", "''")))
            .output()
    } else {
        Command::new("az")
            .arg("account")
            .arg("show")
            .arg("--query")
            .arg("id")
            .arg("--output")
            .arg("tsv")
            .output()
    };
    
    match output {
        Ok(result) => {
            if result.status.success() {
                let sub_id = String::from_utf8_lossy(&result.stdout).trim().to_string();
                if !sub_id.is_empty() {
                    Ok(sub_id)
                } else {
                    Err("No subscription ID found in Azure CLI output".to_string())
                }
            } else {
                Err("Failed to get subscription ID from Azure CLI".to_string())
            }
        }
        Err(e) => Err(format!("Failed to execute Azure CLI: {}", e)),
    }
}

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
    // First, find the repo root using the same logic as find_repo_root()
    let repo_root = find_repo_root()?;
    
    // Build the expected paths relative to repo root - use the exact path we know exists
    let expected_exe = repo_root.join("tools").join("Mystira.DevHub.CLI").join("bin").join("Debug").join("net9.0").join("Mystira.DevHub.CLI.exe");
    let expected_dll = repo_root.join("tools").join("Mystira.DevHub.CLI").join("bin").join("Debug").join("net9.0").join("Mystira.DevHub.CLI.dll");
    
    // Check the primary location first
    if expected_exe.exists() {
        return Ok(expected_exe);
    }
    if expected_dll.exists() {
        return Ok(expected_dll);
    }
    
    // Also try Release configuration
    let release_exe = repo_root.join("tools").join("Mystira.DevHub.CLI").join("bin").join("Release").join("net9.0").join("Mystira.DevHub.CLI.exe");
    let release_dll = repo_root.join("tools").join("Mystira.DevHub.CLI").join("bin").join("Release").join("net9.0").join("Mystira.DevHub.CLI.dll");
    
    if release_exe.exists() {
        return Ok(release_exe);
    }
    if release_dll.exists() {
        return Ok(release_dll);
    }
    
    // Fallback: try relative to current directory
    if let Ok(current_dir) = env::current_dir() {
        let current_exe = current_dir.join("tools").join("Mystira.DevHub.CLI").join("bin").join("Debug").join("net9.0").join("Mystira.DevHub.CLI.exe");
        let current_dll = current_dir.join("tools").join("Mystira.DevHub.CLI").join("bin").join("Debug").join("net9.0").join("Mystira.DevHub.CLI.dll");
        
        if current_exe.exists() {
            return Ok(current_exe);
        }
        if current_dll.exists() {
            return Ok(current_dll);
        }
    }

    // If no built executable found, provide helpful error with the exact path we checked
    Err(format!(
        "Could not find Mystira.DevHub.CLI executable.\n\n\
         Please build the CLI first:\n\
         1. Open a terminal\n\
         2. Navigate to: tools/Mystira.DevHub.CLI\n\
         3. Run: dotnet build\n\n\
         The executable should be at:\n\
         {}\n\n\
         Repo root: {}",
        expected_exe.display(),
        repo_root.display()
    ))
}

// Execute .NET CLI wrapper and return response
async fn execute_devhub_cli(command: String, args: serde_json::Value) -> Result<CommandResponse, String> {
    // Validate command is not empty
    let command_trimmed = command.trim();
    if command_trimmed.is_empty() {
        return Err(format!("Command cannot be empty. Received command: '{}'", command));
    }

    let request = CommandRequest {
        command: command_trimmed.to_string(),
        args,
    };

    let request_json = serde_json::to_string(&request)
        .map_err(|e| format!("Failed to serialize request: {}. Command was: '{}'", e, command_trimmed))?;

    // Get the CLI executable path
    let cli_exe_path = get_cli_executable_path()?;
    
    // Validate the executable exists
    if !cli_exe_path.exists() {
        return Err(format!(
            "CLI executable not found at: {}\n\n\
             Please build the CLI first:\n\
             1. Open a terminal\n\
             2. Navigate to: tools/Mystira.DevHub.CLI\n\
             3. Run: dotnet build",
            cli_exe_path.display()
        ));
    }

    // Spawn the .NET process
    let mut child = Command::new(&cli_exe_path)
        .stdin(Stdio::piped())
        .stdout(Stdio::piped())
        .stderr(Stdio::piped())
        .spawn()
        .map_err(|e| {
            let error_msg = if e.kind() == std::io::ErrorKind::NotFound {
                format!(
                    "Program not found: {}\n\n\
                     The Mystira.DevHub.CLI executable was not found at the expected location.\n\
                     Please build the CLI first:\n\
                     1. Open a terminal\n\
                     2. Navigate to: tools/Mystira.DevHub.CLI\n\
                     3. Run: dotnet build",
                    cli_exe_path.display()
                )
            } else {
                format!("Failed to spawn process at {}: {}", cli_exe_path.display(), e)
            };
            error_msg
        })?;

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
    // Get subscription ID from Azure CLI (fallback to hardcoded if not available)
    let sub_id = get_azure_subscription_id().unwrap_or_else(|_| "22f9eb18-6553-4b7d-9451-47d0195085fe".to_string());
    
    let deployment_path = format!(
        "{}/src/Mystira.App.Infrastructure.Azure/Deployment/{}",
        repo_root, env
    );
    
    // Check if Azure CLI is installed
    if !check_azure_cli_installed() {
        let winget_available = check_winget_available();
        let error_msg = if winget_available {
            "Azure CLI is not installed. You can install it automatically using winget.".to_string()
        } else {
            "Azure CLI is not installed. Please install it manually from https://aka.ms/installazurecliwindows".to_string()
        };
        return Ok(CommandResponse {
            success: false,
            result: Some(serde_json::json!({
                "azureCliMissing": true,
                "wingetAvailable": winget_available,
            })),
            message: None,
            error: Some(error_msg),
        });
    }

    // Get the Azure CLI path
    let program_files = env::var("ProgramFiles").unwrap_or_else(|_| "C:\\Program Files".to_string());
    let az_path = format!("{}\\Microsoft SDKs\\Azure\\CLI2\\wbin\\az.cmd", program_files);
    let az_path_buf = PathBuf::from(&az_path);
    let use_direct_path = az_path_buf.exists();
    
    // Check if logged in
    let account_check = if use_direct_path {
        Command::new("powershell")
            .arg("-NoProfile")
            .arg("-Command")
            .arg(format!("& '{}' account show", az_path.replace("'", "''")))
            .output()
    } else {
        Command::new("az")
            .arg("account")
            .arg("show")
            .output()
    };
    
    if account_check.is_err() || !account_check.unwrap().status.success() {
        return Ok(CommandResponse {
            success: false,
            result: None,
            message: None,
            error: Some("Not logged in to Azure. Please run 'az login' first.".to_string()),
        });
    }
    
    // Set subscription
    let sub_id_for_error = sub_id.clone();
    let set_sub = if use_direct_path {
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
    
    if set_sub.is_err() || !set_sub.unwrap().status.success() {
        return Ok(CommandResponse {
            success: false,
            result: None,
            message: None,
            error: Some(format!("Failed to set subscription: {}", sub_id_for_error)),
        });
    }
    
    // Create resource group if it doesn't exist
    let _rg_create = if use_direct_path {
        Command::new("powershell")
            .arg("-NoProfile")
            .arg("-Command")
            .arg(format!("& '{}' group create --name '{}' --location '{}' --output 'none'", az_path.replace("'", "''"), rg.replace("'", "''"), loc.replace("'", "''")))
            .output()
    } else {
        Command::new("az")
            .arg("group")
            .arg("create")
            .arg("--name")
            .arg(&rg)
            .arg("--location")
            .arg(&loc)
            .arg("--output")
            .arg("none")
            .output()
    };
    
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
    
    // Build parameters JSON string and write to temp file
    let params_json = format!(r#"{{"environment":{{"value":"{}"}},"location":{{"value":"{}"}},"deployStorage":{{"value":{}}},"deployCosmos":{{"value":{}}},"deployAppService":{{"value":{}}}}}"#, 
        env, loc, deploy_storage, deploy_cosmos, deploy_app_service);
    let params_file = format!("{}/params-deploy.json", deployment_path);
    
    // Write parameters to file
    if let Err(e) = fs::write(&params_file, &params_json) {
        return Ok(CommandResponse {
            success: false,
            result: None,
            message: None,
            error: Some(format!("Failed to write parameters file: {}", e)),
        });
    }
    
    // ⚠️ SAFETY: Always use Incremental mode to prevent accidental resource deletion
    // Incremental mode only creates/updates resources in the template, never deletes existing ones
    let deploy_output = if use_direct_path {
        Command::new("powershell")
            .arg("-NoProfile")
            .arg("-Command")
            .arg(format!("Set-Location '{}'; & '{}' deployment group create --resource-group '{}' --template-file 'main.bicep' --parameters '@params-deploy.json' --mode 'Incremental' --name '{}'", 
                deployment_path.replace("'", "''"), az_path.replace("'", "''"), rg.replace("'", "''"), deployment_name.replace("'", "''")))
            .output()
    } else {
        Command::new("az")
            .arg("deployment")
            .arg("group")
            .arg("create")
            .arg("--resource-group")
            .arg(&rg)
            .arg("--template-file")
            .arg(format!("{}/main.bicep", deployment_path))
            .arg("--parameters")
            .arg("@params-deploy.json")
            .arg("--mode")
            .arg("Incremental")
            .arg("--name")
            .arg(&deployment_name)
            .current_dir(&deployment_path)
            .output()
    };
    
    // Clean up temp file
    let _ = fs::remove_file(&params_file);
    
    match deploy_output {
        Ok(output) => {
            if output.status.success() {
                // Get deployment outputs
                let outputs = if use_direct_path {
                    Command::new("powershell")
                        .arg("-NoProfile")
                        .arg("-Command")
                        .arg(format!("& '{}' deployment group show --resource-group '{}' --name '{}' --query 'properties.outputs' --output 'json'", 
                            az_path.replace("'", "''"), rg.replace("'", "''"), deployment_name.replace("'", "''")))
                        .output()
                } else {
                    Command::new("az")
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
                        .output()
                };
                
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
                        // Only count resources that are fully provisioned (Succeeded state)
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

#[tauri::command]
async fn azure_validate_infrastructure(
    repo_root: String,
    environment: String,
    resource_group: Option<String>,
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
    let sub_id = "22f9eb18-6553-4b7d-9451-47d0195085fe";
    
    let deployment_path = format!(
        "{}/src/Mystira.App.Infrastructure.Azure/Deployment/{}",
        repo_root, env
    );
    
    // Check if Azure CLI is installed
    if !check_azure_cli_installed() {
        let winget_available = check_winget_available();
        let error_msg = if winget_available {
            "Azure CLI is not installed. You can install it automatically using winget.".to_string()
        } else {
            "Azure CLI is not installed. Please install it manually from https://aka.ms/installazurecliwindows".to_string()
        };
        return Ok(CommandResponse {
            success: false,
            result: Some(serde_json::json!({
                "azureCliMissing": true,
                "wingetAvailable": winget_available,
            })),
            message: None,
            error: Some(error_msg),
        });
    }

    // Get the Azure CLI path
    let program_files = env::var("ProgramFiles").unwrap_or_else(|_| "C:\\Program Files".to_string());
    let az_path = format!("{}\\Microsoft SDKs\\Azure\\CLI2\\wbin\\az.cmd", program_files);
    let az_path_buf = PathBuf::from(&az_path);
    let use_direct_path = az_path_buf.exists();
    
    // Set subscription
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
    
    // Create resource group if it doesn't exist (needed for validation)
    let _ = if use_direct_path {
        Command::new("powershell")
            .arg("-NoProfile")
            .arg("-Command")
            .arg(format!("& '{}' group create --name '{}' --location 'westeurope' --output 'none'", az_path.replace("'", "''"), rg.replace("'", "''")))
            .output()
    } else {
        Command::new("az")
            .arg("group")
            .arg("create")
            .arg("--name")
            .arg(&rg)
            .arg("--location")
            .arg("westeurope")
            .arg("--output")
            .arg("none")
            .output()
    };
    
    // Validate bicep - write parameters to temp file to avoid PowerShell escaping issues
    let deploy_storage_val = deploy_storage.unwrap_or(true);
    let deploy_cosmos_val = deploy_cosmos.unwrap_or(true);
    let deploy_app_service_val = deploy_app_service.unwrap_or(true);
    let params_json = format!(r#"{{"environment":{{"value":"{}"}},"location":{{"value":"westeurope"}},"deployStorage":{{"value":{}}},"deployCosmos":{{"value":{}}},"deployAppService":{{"value":{}}}}}"#, 
        env, deploy_storage_val, deploy_cosmos_val, deploy_app_service_val);
    let params_file = format!("{}/params-validate.json", deployment_path);
    
    // Write parameters to file
    if let Err(e) = fs::write(&params_file, &params_json) {
        return Ok(CommandResponse {
            success: false,
            result: None,
            message: None,
            error: Some(format!("Failed to write parameters file: {}", e)),
        });
    }
    
    let validate_output = if use_direct_path {
        Command::new("powershell")
            .arg("-NoProfile")
            .arg("-Command")
            .arg(format!("Set-Location '{}'; & '{}' deployment group validate --resource-group '{}' --template-file 'main.bicep' --parameters '@params-validate.json'", 
                deployment_path.replace("'", "''"), az_path.replace("'", "''"), rg.replace("'", "''")))
            .output()
    } else {
        Command::new("az")
            .arg("deployment")
            .arg("group")
            .arg("validate")
            .arg("--resource-group")
            .arg(&rg)
            .arg("--template-file")
            .arg(format!("{}/main.bicep", deployment_path))
            .arg("--parameters")
            .arg("@params-validate.json")
            .current_dir(&deployment_path)
            .output()
    };
    
    // Clean up temp file
    let _ = fs::remove_file(&params_file);
    
    match validate_output {
        Ok(output) => {
            let stdout = String::from_utf8_lossy(&output.stdout);
            let stderr = String::from_utf8_lossy(&output.stderr);
            
            // Check if validation succeeded (exit code 0)
            if output.status.success() {
                // Even on success, there might be warnings - check stderr for warnings
                let warnings = if !stderr.trim().is_empty() {
                    Some(stderr.to_string())
                } else {
                    None
                };
                
                Ok(CommandResponse {
                    success: true,
                    result: Some(serde_json::json!({
                        "message": "Bicep templates are valid",
                        "warnings": warnings,
                        "output": stdout.to_string()
                    })),
                    message: Some("Validation successful".to_string()),
                    error: warnings,
                })
            } else {
                // Validation failed - combine stdout and stderr for full error message
                let error_msg = if !stderr.trim().is_empty() {
                    format!("{}\n{}", stderr, stdout)
                } else {
                    stdout.to_string()
                };
                
                Ok(CommandResponse {
                    success: false,
                    result: None,
                    message: None,
                    error: Some(format!("Validation failed: {}", error_msg)),
                })
            }
        }
        Err(e) => {
            let error_msg = if e.kind() == std::io::ErrorKind::NotFound {
                "Azure CLI not found. Please install Azure CLI first. Visit https://aka.ms/installazurecliwindows for installation instructions.".to_string()
            } else {
                format!("Failed to validate: {}. Make sure Azure CLI is installed and accessible in your PATH.", e)
            };
            Ok(CommandResponse {
            success: false,
            result: None,
            message: None,
                error: Some(error_msg),
            })
        },
    }
}

#[tauri::command]
async fn azure_preview_infrastructure(
    repo_root: String,
    environment: String,
    resource_group: Option<String>,
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
    let sub_id = "22f9eb18-6553-4b7d-9451-47d0195085fe";
    
    let deployment_path = format!(
        "{}/src/Mystira.App.Infrastructure.Azure/Deployment/{}",
        repo_root, env
    );
    
    // Check if Azure CLI is installed
    if !check_azure_cli_installed() {
        let winget_available = check_winget_available();
        let error_msg = if winget_available {
            "Azure CLI is not installed. You can install it automatically using winget.".to_string()
        } else {
            "Azure CLI is not installed. Please install it manually from https://aka.ms/installazurecliwindows".to_string()
        };
        return Ok(CommandResponse {
            success: false,
            result: Some(serde_json::json!({
                "azureCliMissing": true,
                "wingetAvailable": winget_available,
            })),
            message: None,
            error: Some(error_msg),
        });
    }

    // Get the Azure CLI path
    let program_files = env::var("ProgramFiles").unwrap_or_else(|_| "C:\\Program Files".to_string());
    let az_path = format!("{}\\Microsoft SDKs\\Azure\\CLI2\\wbin\\az.cmd", program_files);
    let az_path_buf = PathBuf::from(&az_path);
    let use_direct_path = az_path_buf.exists();
    
    // Set subscription
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
    
    // Create resource group if it doesn't exist (needed for what-if)
    let _ = if use_direct_path {
        Command::new("powershell")
            .arg("-NoProfile")
            .arg("-Command")
            .arg(format!("& '{}' group create --name '{}' --location 'westeurope' --output 'none'", az_path.replace("'", "''"), rg.replace("'", "''")))
            .output()
    } else {
        Command::new("az")
            .arg("group")
            .arg("create")
            .arg("--name")
            .arg(&rg)
            .arg("--location")
            .arg("westeurope")
            .arg("--output")
            .arg("none")
            .output()
    };
    
    // Preview changes (what-if) - write parameters to temp file
    let deploy_storage_val = deploy_storage.unwrap_or(true);
    let deploy_cosmos_val = deploy_cosmos.unwrap_or(true);
    let deploy_app_service_val = deploy_app_service.unwrap_or(true);
    let preview_params_json = format!(r#"{{"environment":{{"value":"{}"}},"location":{{"value":"westeurope"}},"deployStorage":{{"value":{}}},"deployCosmos":{{"value":{}}},"deployAppService":{{"value":{}}}}}"#, 
        env, deploy_storage_val, deploy_cosmos_val, deploy_app_service_val);
    let preview_params_file = format!("{}/params-preview.json", deployment_path);
    
    // Write parameters to file
    if let Err(e) = fs::write(&preview_params_file, &preview_params_json) {
        return Ok(CommandResponse {
            success: false,
            result: None,
            message: None,
            error: Some(format!("Failed to write parameters file: {}", e)),
        });
    }
    
    let preview_output = if use_direct_path {
        Command::new("powershell")
            .arg("-NoProfile")
            .arg("-Command")
            .arg(format!("Set-Location '{}'; & '{}' deployment group what-if --resource-group '{}' --template-file 'main.bicep' --parameters '@params-preview.json' --output 'json'", 
                deployment_path.replace("'", "''"), az_path.replace("'", "''"), rg.replace("'", "''")))
            .output()
    } else {
        Command::new("az")
            .arg("deployment")
            .arg("group")
            .arg("what-if")
            .arg("--resource-group")
            .arg(&rg)
            .arg("--template-file")
            .arg(format!("{}/main.bicep", deployment_path))
            .arg("--parameters")
            .arg("@params-preview.json")
            .arg("--output")
            .arg("json")
            .current_dir(&deployment_path)
            .output()
    };
    
    // Clean up temp file
    let _ = fs::remove_file(&preview_params_file);
    
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
        Err(e) => {
            let error_msg = if e.kind() == std::io::ErrorKind::NotFound {
                "Azure CLI not found. Please install Azure CLI first. Visit https://aka.ms/installazurecliwindows for installation instructions.".to_string()
            } else {
                format!("Failed to preview: {}. Make sure Azure CLI is installed and accessible in your PATH.", e)
            };
            Ok(CommandResponse {
            success: false,
            result: None,
            message: None,
                error: Some(error_msg),
            })
        },
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

// Helper function to check if Azure CLI is installed
fn check_azure_cli_installed() -> bool {
    #[cfg(target_os = "windows")]
    {
        // Method 1: Use 'where' command to find az in PATH (most reliable on Windows)
        let where_check = Command::new("where")
            .arg("az")
            .output();
        
        if let Ok(output) = where_check {
            if output.status.success() {
                let path = String::from_utf8_lossy(&output.stdout);
                if !path.trim().is_empty() {
                    // Found az in PATH, verify it works
                    if Command::new("az")
                        .arg("--version")
                        .output()
                        .is_ok()
                    {
                        return true;
                    }
                }
            }
        }
        
        // Method 2: Use PowerShell Get-Command which checks system PATH
        let ps_check = Command::new("powershell")
            .arg("-NoProfile")
            .arg("-Command")
            .arg("try { $null = Get-Command az -ErrorAction Stop; exit 0 } catch { exit 1 }")
            .output();
        
        if let Ok(output) = ps_check {
            if output.status.success() {
                // Verify it actually works
                if Command::new("az")
                    .arg("--version")
                    .output()
                    .is_ok()
                {
                    return true;
                }
            }
        }
        
        // Method 3: Check common installation locations directly - CHECK THIS FIRST since we know it's installed here
        let program_files = env::var("ProgramFiles").unwrap_or_else(|_| "C:\\Program Files".to_string());
        let known_path = format!("{}\\Microsoft SDKs\\Azure\\CLI2\\wbin\\az.cmd", program_files);
        let path = PathBuf::from(&known_path);
        
        if path.exists() {
            // Use PowerShell to execute it - we know this works
            let check = Command::new("powershell")
                .arg("-NoProfile")
                .arg("-Command")
                .arg(format!("& '{}' --version; if ($?) {{ exit 0 }} else {{ exit 1 }}", known_path.replace("'", "''")))
                .output();
            
            if let Ok(output) = check {
                if output.status.success() {
                    return true;
                }
            }
        }
        
        // Check other common locations
        let program_files_x86 = env::var("ProgramFiles(x86)").unwrap_or_else(|_| "C:\\Program Files (x86)".to_string());
        let common_paths = vec![
            format!("{}\\Microsoft SDKs\\Azure\\CLI2\\wbin\\az.cmd", program_files_x86),
            "C:\\Program Files (x86)\\Microsoft SDKs\\Azure\\CLI2\\wbin\\az.cmd".to_string(),
        ];
        
        for path_str in common_paths {
            let path = PathBuf::from(&path_str);
            if path.exists() {
                let check = Command::new("powershell")
                    .arg("-NoProfile")
                    .arg("-Command")
                    .arg(format!("& '{}' --version; if ($?) {{ exit 0 }} else {{ exit 1 }}", path_str.replace("'", "''")))
                    .output();
                
                if let Ok(output) = check {
                    if output.status.success() {
                        return true;
                    }
                }
            }
        }
        
        // Method 4: Check user's local AppData location
        if let Ok(local_appdata) = env::var("LOCALAPPDATA") {
            let user_path = format!("{}\\Microsoft\\AzureCLI2\\wbin\\az.cmd", local_appdata);
            let path = PathBuf::from(&user_path);
            if path.exists() {
                let check = Command::new("powershell")
                    .arg("-NoProfile")
                    .arg("-Command")
                    .arg(format!("& '{}' --version; if ($?) {{ exit 0 }} else {{ exit 1 }}", user_path.replace("'", "''")))
                    .output();
                
                if let Ok(output) = check {
                    if output.status.success() {
                        return true;
                    }
                }
            }
        }
        
        // Method 5: Try running az directly - sometimes it works even if not in visible PATH
        if Command::new("az")
            .arg("--version")
            .output()
            .is_ok()
        {
            return true;
        }
    }
    
    #[cfg(not(target_os = "windows"))]
    {
        // On non-Windows, just check PATH
        if Command::new("az")
            .arg("--version")
            .output()
            .is_ok()
        {
            return true;
        }
    }
    
    false
}

// Helper function to check if winget is available
fn check_winget_available() -> bool {
    #[cfg(target_os = "windows")]
    {
        Command::new("winget")
            .arg("--version")
            .output()
            .is_ok()
    }
    #[cfg(not(target_os = "windows"))]
    {
        false
    }
}

#[tauri::command]
async fn check_azure_cli() -> Result<CommandResponse, String> {
    let is_installed = check_azure_cli_installed();
    let winget_available = check_winget_available();
    
    Ok(CommandResponse {
        success: is_installed,
        result: Some(serde_json::json!({
            "installed": is_installed,
            "wingetAvailable": winget_available,
        })),
        message: if is_installed {
            Some("Azure CLI is installed".to_string())
        } else {
            Some("Azure CLI is not installed".to_string())
        },
        error: None,
    })
}

#[tauri::command]
async fn install_azure_cli() -> Result<CommandResponse, String> {
    #[cfg(target_os = "windows")]
    {
        // Check if winget is available
        if !check_winget_available() {
            return Ok(CommandResponse {
                success: false,
                result: None,
                message: None,
                error: Some("winget is not available. Please install Azure CLI manually from https://aka.ms/installazurecliwindows".to_string()),
            });
        }
        
        // Install Azure CLI via winget in a visible terminal window
        // Use cmd /c start to open a new visible PowerShell window that stays open
        let spawn_result = Command::new("cmd")
            .arg("/c")
            .arg("start")
            .arg("PowerShell")
            .arg("-NoExit")
            .arg("-Command")
            .arg("winget install Microsoft.AzureCLI --accept-package-agreements --accept-source-agreements; Write-Host 'Installation complete. You can close this window.'; pause")
            .spawn();
        
        match spawn_result {
            Ok(_) => {
                Ok(CommandResponse {
                    success: true,
                    result: Some(serde_json::json!({
                        "message": "A terminal window has opened to install Azure CLI. After installation completes, please RESTART the application for Azure CLI to be detected.",
                        "requiresRestart": true
                    })),
                    message: Some("Azure CLI installation window opened. Please restart the app after installation.".to_string()),
                    error: None,
                })
            }
            Err(e) => Ok(CommandResponse {
                success: false,
                result: None,
                message: None,
                error: Some(format!("Failed to open installation window: {}. Please install Azure CLI manually from https://aka.ms/installazurecliwindows", e)),
            }),
        }
    }
    
    #[cfg(not(target_os = "windows"))]
    {
        Ok(CommandResponse {
            success: false,
            result: None,
            message: None,
            error: Some("Automatic installation is only available on Windows. Please install Azure CLI manually: https://docs.microsoft.com/cli/azure/install-azure-cli".to_string()),
        })
    }
}

#[tauri::command]
async fn delete_azure_resource(resource_id: String) -> Result<CommandResponse, String> {
    // Check if Azure CLI is installed first
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
    // Format: /subscriptions/{sub}/resourceGroups/{rg}/providers/{type}/{name}
    let parts: Vec<&str> = resource_id.split('/').collect();
    let mut resource_group = String::new();
    let mut resource_name = String::new();
    
    for (i, part) in parts.iter().enumerate() {
        if part == &"resourceGroups" && i + 1 < parts.len() {
            resource_group = parts[i + 1].to_string();
        }
        if i > 0 && parts[i - 1] == "providers" && i < parts.len() {
            // Resource name is typically the last part after providers/{type}/
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

    // Get the Azure CLI path
    let program_files = env::var("ProgramFiles").unwrap_or_else(|_| "C:\\Program Files".to_string());
    let az_path = format!("{}\\Microsoft SDKs\\Azure\\CLI2\\wbin\\az.cmd", program_files);
    let az_path_buf = PathBuf::from(&az_path);
    let use_direct_path = az_path_buf.exists();

    // Delete resource using Azure CLI
    let delete_output = if use_direct_path {
        Command::new("powershell")
            .arg("-NoProfile")
            .arg("-Command")
            .arg(format!("& '{}' resource delete --ids '{}' --yes", az_path.replace("'", "''"), resource_id.replace("'", "''")))
            .output()
    } else {
        Command::new("az")
            .arg("resource")
            .arg("delete")
            .arg("--ids")
            .arg(&resource_id)
            .arg("--yes")
            .output()
    };

    match delete_output {
        Ok(output) => {
            if output.status.success() {
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
                Ok(CommandResponse {
                    success: false,
                    result: None,
                    message: None,
                    error: Some(format!("Failed to delete resource: {}", error_msg)),
                })
            }
        }
        Err(e) => Ok(CommandResponse {
            success: false,
            result: None,
            message: None,
            error: Some(format!("Failed to delete resource: {}", e)),
        }),
    }
}

#[tauri::command]
async fn get_azure_resources(subscription_id: Option<String>) -> Result<CommandResponse, String> {
    // Check if Azure CLI is installed first
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

    // Get the Azure CLI path
    let program_files = env::var("ProgramFiles").unwrap_or_else(|_| "C:\\Program Files".to_string());
    let az_path = format!("{}\\Microsoft SDKs\\Azure\\CLI2\\wbin\\az.cmd", program_files);
    let az_path_buf = PathBuf::from(&az_path);
    let use_direct_path = az_path_buf.exists();

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
                
                // Parse JSON response
                let resources: Result<Vec<serde_json::Value>, _> = serde_json::from_str(&stdout);
                
                match resources {
                    Ok(resources_vec) => {
                        // Transform to expected format
                        let transformed: Vec<serde_json::Value> = resources_vec.iter().map(|r| {
                            serde_json::json!({
                                "id": r.get("id").and_then(|v| v.as_str()).unwrap_or(""),
                                "name": r.get("name").and_then(|v| v.as_str()).unwrap_or(""),
                                "type": r.get("type").and_then(|v| v.as_str()).unwrap_or(""),
                                "location": r.get("location").and_then(|v| v.as_str()),
                                "resourceGroup": r.get("resourceGroup").and_then(|v| v.as_str()),
                                "sku": r.get("sku"),
                                "kind": r.get("kind").and_then(|v| v.as_str()),
                            })
                        }).collect();

                        Ok(CommandResponse {
                            success: true,
                            result: Some(serde_json::json!(transformed)),
                            message: Some(format!("Found {} resources", transformed.len())),
                            error: None,
                        })
                    }
                    Err(e) => Ok(CommandResponse {
                        success: false,
                        result: None,
                        message: None,
                        error: Some(format!("Failed to parse Azure CLI response: {}. Output: {}", e, stdout)),
                    }),
                }
            } else {
                let stderr = String::from_utf8_lossy(&result.stderr);
                let stdout = String::from_utf8_lossy(&result.stdout);
                Ok(CommandResponse {
                    success: false,
                    result: None,
                    message: None,
                    error: Some(format!("Azure CLI error: {}\n{}", stderr, stdout)),
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

#[tauri::command]
async fn check_infrastructure_status(
    _environment: String,
    resource_group: String,
) -> Result<CommandResponse, String> {
    // Check if Azure CLI is installed
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

    // Set subscription (required for resource queries)
    // Get subscription ID from Azure CLI (fallback to hardcoded if not available)
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

    // Get resources in the resource group
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
                        // Check for specific resource types
                        let mut status = serde_json::json!({
                            "available": false, // Will be set to true only if required resources exist
                            "resources": {
                                "storage": { "exists": false, "health": "unknown", "instances": [] },
                                "cosmos": { "exists": false, "health": "unknown", "instances": [] },
                                "appService": { "exists": false, "health": "unknown", "instances": [] },
                                "keyVault": { "exists": false, "health": "unknown", "instances": [] }
                            },
                            "lastChecked": std::time::SystemTime::now().duration_since(std::time::UNIX_EPOCH).unwrap().as_secs() * 1000,
                            "resourceGroup": resource_group
                        });

                        // Track all instances of each resource type
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
                            
                            // Get actual runtime status for App Service
                            let mut runtime_status = "unknown".to_string();
                            let mut runtime_health = "unknown".to_string();
                            
                            if resource_type == "Microsoft.Web/sites" {
                                // Try to get site state
                                if let Some(properties) = resource.get("properties") {
                                    if let Some(state) = properties.get("state") {
                                        runtime_status = state.as_str().unwrap_or("unknown").to_string();
                                    }
                                }
                                // Health check: Running = healthy, Stopped = unhealthy, etc.
                                runtime_health = match runtime_status.as_str() {
                                    "Running" => "healthy",
                                    "Stopped" => "unhealthy",
                                    "Starting" | "Stopping" => "degraded",
                                    _ => "unknown"
                                }.to_string();
                            }
                            
                            // Determine health based on provisioning state and runtime status
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
                            
                            // Only mark resources as existing if provisioning state is Succeeded
                            // This prevents false positives from resources that are still being created or failed
                            let is_provisioned = provisioning_state == "Succeeded";
                            
                            // Match exact resource types (not just contains) to avoid false positives
                            if resource_type == "Microsoft.Storage/storageAccounts" && is_provisioned {
                                storage_instances.push(instance);
                                status["resources"]["storage"]["exists"] = serde_json::json!(true);
                                // Use first instance name for backward compatibility
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
                                // For App Service, we already have runtime status from properties
                                // The health endpoint check can be done separately via check_resource_health_endpoint
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
                        
                        // Set instances arrays
                        status["resources"]["storage"]["instances"] = serde_json::json!(storage_instances);
                        status["resources"]["cosmos"]["instances"] = serde_json::json!(cosmos_instances);
                        status["resources"]["appService"]["instances"] = serde_json::json!(appservice_instances);
                        status["resources"]["keyVault"]["instances"] = serde_json::json!(keyvault_instances);

                        // Set available to true only if at least one required resource exists and is provisioned
                        // Required resources are: storage, cosmos, or appService
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
                // Resource group might not exist - return empty status
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

#[tauri::command]
async fn get_github_deployments(repository: String, limit: Option<i32>) -> Result<CommandResponse, String> {
    let args = serde_json::json!({
        "repository": repository,
        "limit": limit.unwrap_or(10)
    });
    execute_devhub_cli("github.list-deployments".to_string(), args).await
}

#[tauri::command]
async fn github_dispatch_workflow(workflow_file: String, inputs: Option<serde_json::Value>) -> Result<CommandResponse, String> {
    let args = serde_json::json!({
        "workflowFile": workflow_file,
        "inputs": inputs.unwrap_or(serde_json::json!({}))
    });
    execute_devhub_cli("github.dispatch-workflow".to_string(), args).await
}

#[tauri::command]
async fn github_workflow_status(run_id: i64) -> Result<CommandResponse, String> {
    let args = serde_json::json!({
        "runId": run_id
    });
    execute_devhub_cli("github.workflow-status".to_string(), args).await
}

#[tauri::command]
async fn github_workflow_logs(run_id: i64) -> Result<CommandResponse, String> {
    let args = serde_json::json!({
        "runId": run_id
    });
    execute_devhub_cli("github.workflow-logs".to_string(), args).await
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
    for (svc_name, pid_opt, port) in &services_to_stop {
        // Remove from services map first
        {
            let mut services_guard = services.lock().map_err(|e| format!("Lock error: {}", e))?;
            services_guard.remove(svc_name);
        }
        
        // Kill the process
        #[cfg(target_os = "windows")]
        {
            if let Some(pid_val) = *pid_opt {
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
                        &format!("Get-NetTCPConnection -LocalPort {} -ErrorAction SilentlyContinue | Select-Object -ExpandProperty OwningProcess | ForEach-Object {{ Stop-Process -Id $_ -Force }}", *port)
                    ])
                    .output();
                tokio::time::sleep(tokio::time::Duration::from_millis(2000)).await;
            }
        }
        
        #[cfg(not(target_os = "windows"))]
        {
            if let Some(pid_val) = *pid_opt {
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

// Helper function to find repository root
fn find_repo_root() -> Result<PathBuf, String> {
    // Try to get the executable's directory first (more reliable in Tauri apps)
    let mut start_dirs = Vec::new();
    
    // Try using std::env::current_exe() which works in Tauri apps
    if let Ok(exe_path) = std::env::current_exe() {
        if let Some(exe_dir) = exe_path.parent() {
            start_dirs.push(exe_dir.to_path_buf());
            // Also try parent directories of the executable
            if let Some(parent) = exe_dir.parent() {
                start_dirs.push(parent.to_path_buf());
            }
            if let Some(grandparent) = exe_dir.parent().and_then(|p| p.parent()) {
                start_dirs.push(grandparent.to_path_buf());
            }
        }
    }
    
    // Also try current working directory
    if let Ok(current_dir) = env::current_dir() {
        start_dirs.push(current_dir);
    }
    
    // Try each starting directory
    for start_dir in &start_dirs {
        // Check if this directory is the repo root
        if start_dir.join(".git").exists() || start_dir.join("Mystira.App.sln").exists() {
            return Ok(start_dir.clone());
        }
        
        // If we're in tools/Mystira.DevHub/src-tauri, go up to repo root
        let start_str = start_dir.to_string_lossy().to_string();
        if start_str.contains("src-tauri") {
            let mut check_dir = start_dir.clone();
            for _ in 0..4 {
                if check_dir.join(".git").exists() || check_dir.join("Mystira.App.sln").exists() {
                    return Ok(check_dir);
                }
                if let Some(parent) = check_dir.parent() {
                    check_dir = parent.to_path_buf();
                } else {
                    break;
                }
            }
    }
    
    // If we're in tools/Mystira.DevHub, go up two levels
        if start_str.ends_with("tools/Mystira.DevHub") || start_str.ends_with("tools\\Mystira.DevHub") {
            if let Some(repo_root) = start_dir.parent()
            .and_then(|p| p.parent()) {
            if repo_root.join(".git").exists() || repo_root.join("Mystira.App.sln").exists() {
                    return Ok(repo_root.to_path_buf());
                }
            }
        }
        
        // Try walking up the directory tree
        let mut check_dir = start_dir.clone();
        for _ in 0..10 {
            if check_dir.join(".git").exists() || check_dir.join("Mystira.App.sln").exists() {
                return Ok(check_dir);
            }
            if let Some(parent) = check_dir.parent() {
                check_dir = parent.to_path_buf();
            } else {
                break;
            }
        }
    }
    
    Err("Could not find repository root. Please ensure you're running from within the repository.".to_string())
}

#[tauri::command]
async fn get_cli_build_time() -> Result<Option<i64>, String> {
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

#[tauri::command]
async fn build_cli() -> Result<CommandResponse, String> {
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

#[tauri::command]
async fn read_bicep_file(relative_path: String) -> Result<String, String> {
    // Find repo root
    let repo_root = find_repo_root()?;
    
    // Resolve the file path relative to repo root
    let file_path = repo_root.join(&relative_path);
    
    // Security: Ensure the path is within the repo root (prevent directory traversal)
    // Normalize paths to handle different separators and symlinks
    let repo_root_canonical = repo_root.canonicalize()
        .map_err(|e| format!("Failed to canonicalize repo root: {}", e))?;
    let file_path_canonical = file_path.canonicalize()
        .map_err(|e| format!("Failed to canonicalize file path: {}", e))?;
    
    if !file_path_canonical.starts_with(&repo_root_canonical) {
        return Err(format!("Invalid path: path must be within repository root"));
    }
    
    // Check if file exists
    if !file_path.exists() {
        return Err(format!("File not found: {}", relative_path));
    }
    
    // Read the file
    fs::read_to_string(&file_path)
        .map_err(|e| format!("Failed to read file {}: {}", relative_path, e))
}

#[tauri::command]
async fn get_repo_root() -> Result<String, String> {
    find_repo_root()
        .map(|p| p.to_string_lossy().to_string())
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
async fn list_github_workflows(environment: Option<String>) -> Result<CommandResponse, String> {
    let repo_root = find_repo_root()?;
    let workflows_path = repo_root.join(".github").join("workflows");
    
    if !workflows_path.exists() {
        return Ok(CommandResponse {
            success: false,
            result: None,
            message: None,
            error: Some(format!("Workflows directory not found: {}", workflows_path.display())),
        });
    }
    
    let mut workflows: Vec<String> = Vec::new();
    
    match fs::read_dir(&workflows_path) {
        Ok(entries) => {
            for entry in entries {
                if let Ok(entry) = entry {
                    let path = entry.path();
                    if path.is_file() {
                        if let Some(ext) = path.extension() {
                            if ext == "yml" || ext == "yaml" {
                                if let Some(file_name) = path.file_name() {
                                    let file_name_str = file_name.to_string_lossy().to_string();
                                    
                                    // Filter by environment if provided
                                    // Match patterns like: mystira-app-api-cicd-{env}.yml or infrastructure-deploy-{env}.yml
                                    // Use case-insensitive matching
                                    if let Some(env) = &environment {
                                        let file_name_lower = file_name_str.to_lowercase();
                                        let env_lower = env.to_lowercase();
                                        
                                        // More precise matching: environment should be surrounded by - or at end of filename
                                        let env_pattern = format!("-{}-", env_lower);
                                        let env_pattern_end = format!("-{}.yml", env_lower);
                                        let env_pattern_end_yaml = format!("-{}.yaml", env_lower);
                                        
                                        if file_name_lower.contains(&env_pattern) || 
                                           file_name_lower.ends_with(&env_pattern_end) ||
                                           file_name_lower.ends_with(&env_pattern_end_yaml) {
                                            workflows.push(file_name_str);
                                        }
                                    } else {
                                        workflows.push(file_name_str);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        Err(e) => {
            return Ok(CommandResponse {
                success: false,
                result: None,
                message: None,
                error: Some(format!("Failed to read workflows directory: {}", e)),
            });
        }
    }
    
    workflows.sort();
    
    Ok(CommandResponse {
        success: true,
        result: Some(serde_json::json!(workflows)),
        message: None,
        error: None,
    })
}

#[tauri::command]
async fn check_resource_health_endpoint(
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
            check_infrastructure_status,
            get_azure_resources,
            delete_azure_resource,
            check_azure_cli,
            install_azure_cli,
            get_github_deployments,
            github_dispatch_workflow,
            github_workflow_status,
            github_workflow_logs,
            test_connection,
            prebuild_service,
            start_service,
            stop_service,
            get_service_status,
            get_repo_root,
            read_bicep_file,
            build_cli,
            get_cli_build_time,
            get_current_branch,
            create_webview_window,
            check_port_available,
            check_service_health,
            get_service_port,
            update_service_port,
            find_available_port,
            list_github_workflows,
            check_resource_health_endpoint,
        ])
        .run(tauri::generate_context!())
        .expect("error while running tauri application");
}
