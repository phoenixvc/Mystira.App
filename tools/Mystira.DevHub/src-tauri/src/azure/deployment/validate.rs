// Azure infrastructure validation command

use crate::azure::deployment::helpers::{
    check_azure_cli_or_error, get_deployment_path, get_resource_group_name,
    set_azure_subscription, ensure_resource_group, build_parameters_json,
};
use crate::helpers::get_azure_cli_path;
use crate::types::CommandResponse;
use std::fs;
use std::process::Command;

/// Validate Azure infrastructure Bicep templates
#[tauri::command]
pub async fn azure_validate_infrastructure(
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
    
    // Create resource group if it doesn't exist (needed for validation)
    let _ = ensure_resource_group(&rg, "southafricanorth");
    
    let deploy_storage_val = deploy_storage.unwrap_or(true);
    let deploy_cosmos_val = deploy_cosmos.unwrap_or(true);
    let deploy_app_service_val = deploy_app_service.unwrap_or(true);
    let params_json = build_parameters_json(env, "southafricanorth", deploy_storage_val, deploy_cosmos_val, deploy_app_service_val);
    let params_file = format!("{}/params-validate.json", deployment_path);
    
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
    
    let _ = fs::remove_file(&params_file);
    
    match validate_output {
        Ok(output) => {
            let stdout = String::from_utf8_lossy(&output.stdout);
            let stderr = String::from_utf8_lossy(&output.stderr);
            
            if output.status.success() {
                let warnings = if !stderr.trim().is_empty() {
                    Some(stderr.to_string())
                } else {
                    None
                };
                
                // Parse output to check for diagnostics/warnings in the JSON
                let mut diagnostic_warnings = warnings.clone();
                if let Ok(output_json) = serde_json::from_str::<serde_json::Value>(&stdout) {
                    if let Some(properties) = output_json.get("properties") {
                        if let Some(diagnostics) = properties.get("diagnostics") {
                            if let Some(diag_array) = diagnostics.as_array() {
                                let diag_messages: Vec<String> = diag_array
                                    .iter()
                                    .filter_map(|d| {
                                        d.get("message").and_then(|m| m.as_str()).map(|s| s.to_string())
                                    })
                                    .collect();
                                if !diag_messages.is_empty() {
                                    let diag_text = diag_messages.join("\n");
                                    diagnostic_warnings = Some(match diagnostic_warnings {
                                        Some(existing) => format!("{}\n{}", existing, diag_text),
                                        None => diag_text,
                                    });
                                }
                            }
                        }
                    }
                }
                
                Ok(CommandResponse {
                    success: true,
                    result: Some(serde_json::json!({
                        "message": "Bicep templates are valid",
                        "warnings": diagnostic_warnings,
                        "output": stdout.to_string()
                    })),
                    message: Some(if let Some(ref w) = diagnostic_warnings {
                        format!("Validation successful with warnings")
                    } else {
                        "Validation successful".to_string()
                    }),
                    error: None, // Warnings should not be in error field - they're in result.warnings
                })
            } else {
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

