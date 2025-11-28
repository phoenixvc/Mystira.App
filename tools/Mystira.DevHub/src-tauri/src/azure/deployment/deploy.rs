// Azure infrastructure deployment command

use crate::azure::deployment::helpers::{
    check_azure_cli_or_error, check_azure_login, get_deployment_path, get_resource_group_name,
    get_subscription_id, set_azure_subscription, ensure_resource_group, build_parameters_json,
};
use crate::helpers::get_azure_cli_path;
use crate::types::CommandResponse;
use serde_json::Value;
use std::fs;
use std::process::Command;
use tracing::{info, warn, error, debug};

/// Deploy Azure infrastructure using Bicep templates
#[tauri::command]
pub async fn azure_deploy_infrastructure(
    repo_root: String,
    environment: String,
    resource_group: Option<String>,
    location: Option<String>,
    deploy_storage: Option<bool>,
    deploy_cosmos: Option<bool>,
    deploy_app_service: Option<bool>,
) -> Result<CommandResponse, String> {
    info!("Starting Azure infrastructure deployment: env={}, repo_root={}", environment, repo_root);
    
    let env = environment.as_str();
    let rg = resource_group.unwrap_or_else(|| get_resource_group_name(env));
    let loc = location.unwrap_or_else(|| "westeurope".to_string());
    let sub_id = get_subscription_id();
    
    debug!("Deployment config: resource_group={}, location={}, subscription={}", rg, loc, sub_id);
    
    let deployment_path = get_deployment_path(&repo_root, env);
    
    // Check Azure CLI installation
    if let Some(error_response) = check_azure_cli_or_error() {
        error!("Azure CLI check failed for deployment");
        return Ok(error_response);
    }

    // Check if logged in
    if let Err(error_response) = check_azure_login() {
        error!("Azure login check failed for deployment");
        return Ok(error_response);
    }

    // Set subscription
    if let Err(e) = set_azure_subscription(&sub_id) {
        error!("Failed to set Azure subscription: {}", e);
        return Ok(CommandResponse {
            success: false,
            result: None,
            message: None,
            error: Some(e),
        });
    }
    
    // Create resource group if it doesn't exist
    debug!("Ensuring resource group exists: {}", rg);
    let _ = ensure_resource_group(&rg, &loc);
    
    // Deploy using bicep
    use std::time::{SystemTime, UNIX_EPOCH};
    let timestamp = SystemTime::now()
        .duration_since(UNIX_EPOCH)
        .unwrap()
        .as_secs();
    let deployment_name = format!("mystira-app-{}-{}", env, timestamp);
    
    let deploy_storage = deploy_storage.unwrap_or(true);
    let deploy_cosmos = deploy_cosmos.unwrap_or(true);
    let deploy_app_service = deploy_app_service.unwrap_or(true);
    
    info!("Deployment components: storage={}, cosmos={}, app_service={}", deploy_storage, deploy_cosmos, deploy_app_service);
    
    // Validate dependencies: App Service requires Cosmos and Storage
    if deploy_app_service && (!deploy_cosmos || !deploy_storage) {
        warn!("Dependency validation failed: App Service requires Cosmos DB and Storage");
        return Ok(CommandResponse {
            success: false,
            result: None,
            message: None,
            error: Some("App Service requires Cosmos DB and Storage Account to be deployed. Please select all dependencies.".to_string()),
        });
    }
    
    // Build parameters JSON string and write to temp file
    let params_json = build_parameters_json(env, &loc, deploy_storage, deploy_cosmos, deploy_app_service);
    let params_file = format!("{}/params-deploy.json", deployment_path);
    
    if let Err(e) = fs::write(&params_file, &params_json) {
        return Ok(CommandResponse {
            success: false,
            result: None,
            message: None,
            error: Some(format!("Failed to write parameters file: {}", e)),
        });
    }
    
    let (az_path, use_direct_path) = get_azure_cli_path();
    
    info!("Starting Azure deployment: name={}, resource_group={}", deployment_name, rg);
    
    // ⚠️ SAFETY: Always use Incremental mode to prevent accidental resource deletion
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
                info!("Azure deployment command executed successfully");
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
                
                info!("Azure deployment completed successfully: deployment={}, resource_group={}", deployment_name, rg);
                
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
                error!("Azure deployment failed: {}", error_msg);
                Ok(CommandResponse {
                    success: false,
                    result: None,
                    message: None,
                    error: Some(format!("Deployment failed: {}", error_msg)),
                })
            }
        }
        Err(e) => {
            error!("Failed to execute Azure deployment command: {}", e);
            Ok(CommandResponse {
                success: false,
                result: None,
                message: None,
                error: Some(format!("Failed to execute deployment: {}", e)),
            })
        },
    }
}

