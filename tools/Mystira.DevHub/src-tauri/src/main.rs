// Prevents additional console window on Windows in release, DO NOT REMOVE!!
#![cfg_attr(not(debug_assertions), windows_subsystem = "windows")]

// Module declarations
mod types;
mod helpers;
mod cli;
mod cosmos;
mod infrastructure;
mod github;
mod azure;
mod services;
mod utils;
mod config;
mod retry;
mod cache;
mod rate_limit;

// Re-export commonly used types
use types::ServiceManager;
use std::sync::{Arc, Mutex};
use std::collections::HashMap;

// Re-export command functions from modules
use cosmos::{cosmos_export, cosmos_stats, migration_run};
use infrastructure::{infrastructure_validate, infrastructure_preview, infrastructure_deploy, infrastructure_destroy, infrastructure_status};
use github::{get_github_deployments, github_dispatch_workflow, github_workflow_status, github_workflow_logs, list_github_workflows};
use azure::cli::{check_azure_cli, install_azure_cli};
use azure::deployment::{azure_deploy_infrastructure, azure_validate_infrastructure, azure_preview_infrastructure, check_infrastructure_exists, check_infrastructure_status, azure_create_resource_group};
use azure::deploy_now::{check_azure_login, check_github_pat, check_swa_cli, check_npm, scan_existing_resources, get_git_status, git_stage_all, git_commit, git_commit_empty, git_push, git_sync, update_cors_settings, restart_api_services, disconnect_swa_cicd, get_swa_deployment_token};
use azure::resources::{get_azure_resources, delete_azure_resource, check_subscription_owner};
use services::lifecycle::{prebuild_service, start_service, stop_service};
use services::status::{get_service_status, check_service_health};
use services::ports::{check_port_available, get_service_port, update_service_port, find_available_port};
use utils::{test_connection, get_cli_build_time, build_cli, read_bicep_file, get_repo_root, get_current_branch, check_resource_health_endpoint, create_webview_window};
use config::{get_app_config, save_app_config, reload_config};

fn main() {
    // Initialize logging
    tracing_subscriber::fmt()
        .with_env_filter(tracing_subscriber::EnvFilter::from_default_env())
        .init();
    
    tracing::info!("Mystira DevHub starting...");
    
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
            azure_create_resource_group,
            azure_validate_infrastructure,
            azure_preview_infrastructure,
            check_infrastructure_exists,
            check_infrastructure_status,
            get_azure_resources,
            delete_azure_resource,
            check_subscription_owner,
            check_azure_cli,
            install_azure_cli,
            check_azure_login,
            check_github_pat,
            check_swa_cli,
            check_npm,
            scan_existing_resources,
            get_git_status,
            git_stage_all,
            git_commit,
            git_commit_empty,
            git_push,
            git_sync,
            update_cors_settings,
            restart_api_services,
            disconnect_swa_cicd,
            get_swa_deployment_token,
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
            get_app_config,
            save_app_config,
            reload_config,
        ])
        .run(tauri::generate_context!())
        .expect("error while running tauri application");
}
