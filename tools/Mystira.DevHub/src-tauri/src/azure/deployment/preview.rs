// Azure infrastructure preview command

use crate::azure::deployment::helpers::{
    check_azure_cli_or_error, get_deployment_path, get_resource_group_name,
    set_azure_subscription, ensure_resource_group, build_parameters_json,
};
use crate::helpers::get_azure_cli_path;
use crate::types::CommandResponse;
use serde_json::Value;
use std::fs;
use std::process::Command;

/// Preview Azure infrastructure changes using what-if
#[tauri::command]
pub async fn azure_preview_infrastructure(
    repo_root: String,
    environment: String,
    resource_group: Option<String>,
    deploy_storage: Option<bool>,
    deploy_cosmos: Option<bool>,
    deploy_app_service: Option<bool>,
) -> Result<CommandResponse, String> {
    let env = environment.as_str();
    let rg = resource_group.unwrap_or_else(|| get_resource_group_name(env));
    let sub_id = "22f9eb18-6553-4b7d-9451-47d0195085fe";
    
    let deployment_path = get_deployment_path(&repo_root, env);
    
    // Check Azure CLI installation
    if let Some(error_response) = check_azure_cli_or_error() {
        return Ok(error_response);
    }

    let (az_path, use_direct_path) = get_azure_cli_path();
    
    // Set subscription
    let _ = set_azure_subscription(sub_id);
    
    // Create resource group if it doesn't exist (needed for what-if)
    let _ = ensure_resource_group(&rg, "westeurope");
    
    let deploy_storage_val = deploy_storage.unwrap_or(true);
    let deploy_cosmos_val = deploy_cosmos.unwrap_or(true);
    let deploy_app_service_val = deploy_app_service.unwrap_or(true);
    let preview_params_json = build_parameters_json(env, "westeurope", deploy_storage_val, deploy_cosmos_val, deploy_app_service_val);
    let preview_params_file = format!("{}/params-preview.json", deployment_path);
    
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
    
    let _ = fs::remove_file(&preview_params_file);
    
    match preview_output {
        Ok(output) => {
            let stdout = String::from_utf8_lossy(&output.stdout);
            let stderr = String::from_utf8_lossy(&output.stderr);
            
            let stderr_str = stderr.to_string();
            // Check for Cosmos DB nested resource errors
            // These occur when Azure what-if tries to query nested resources (databases/containers) that don't exist yet
            let has_cosmos_nested_resources = stderr_str.contains("Microsoft.DocumentDB") 
                && (stderr_str.contains("sqlDatabases") || stderr_str.contains("containers"));
            let has_whatif_errors = stderr_str.contains("DeploymentWhatIfResourceError");
            let has_invalid_response = stderr_str.contains("DeploymentWhatIfResourceInvalidResponse");
            let error_count_whatif = stderr_str.matches("DeploymentWhatIfResourceError").count();
            let error_count_invalid = stderr_str.matches("DeploymentWhatIfResourceInvalidResponse").count();
            let total_cosmos_errors = error_count_whatif + error_count_invalid;
            
            // Consider it only Cosmos DB nested resource errors if:
            // 1. All errors relate to Cosmos DB nested resources (databases/containers)
            // 2. Errors are either WhatIfResourceError or InvalidResponse (both occur for nested resources)
            // 3. Error count is reasonable (not too many different errors - max 20 for multiple containers)
            // 4. Ensure all DocumentDB mentions are actually error-related (not other references)
            let cosmos_error_pattern_count = stderr_str.matches("Microsoft.DocumentDB").count();
            let is_only_cosmos_errors = has_cosmos_nested_resources
                && (has_whatif_errors || has_invalid_response)
                && total_cosmos_errors <= 20
                && cosmos_error_pattern_count >= total_cosmos_errors; // All errors must reference DocumentDB
            
            let parsed_json: Option<Value> = serde_json::from_str(&stdout).ok();
            let has_valid_preview = parsed_json.is_some() && parsed_json.as_ref().and_then(|v| v.get("changes")).is_some();
            
            // If errors are ONLY Cosmos DB nested resource errors, treat as success even without valid preview JSON
            // This is because Azure what-if can't query nested resources that don't exist yet, but deployment will still work
            let is_success = output.status.success() || is_only_cosmos_errors;
            
            // Filter errors if they're only Cosmos DB nested resource errors (even without valid preview JSON)
            let filtered_errors = if is_only_cosmos_errors {
                None // Filter out Cosmos DB nested resource errors - they're false positives
            } else if !output.status.success() {
                Some(stderr_str.clone())
            } else {
                None
            };
            
            // Create appropriate warning message
            let warning_message = if is_only_cosmos_errors {
                if has_valid_preview {
                    Some("Preview generated with warnings: Cosmos DB nested resources (databases/containers) may show errors if they don't exist yet. This is expected and won't prevent deployment.".to_string())
                } else {
                    Some("Preview partially generated: Azure what-if analysis cannot query Cosmos DB nested resources (databases/containers) that don't exist yet. These errors are expected and won't prevent deployment. The deployment will create these resources successfully.".to_string())
                }
            } else {
                None
            };
            
            Ok(CommandResponse {
                success: is_success,
                result: Some(serde_json::json!({
                    "preview": stdout.to_string(),
                    "parsed": parsed_json,
                    "errors": filtered_errors,
                    "warnings": if is_only_cosmos_errors { 
                        Some("Cosmos DB nested resource errors are expected when resources don't exist yet. Deployment will still proceed successfully.") 
                    } else { 
                        None 
                    }
                })),
                message: if is_success {
                    warning_message.or(Some("Preview generated successfully".to_string()))
                } else {
                    None
                },
                error: filtered_errors,
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

