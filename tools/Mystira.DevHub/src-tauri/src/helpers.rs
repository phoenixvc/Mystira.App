//! Utility functions for common operations.
//!
//! This module provides helper functions for:
//! - Azure CLI path detection and subscription management
//! - Repository root discovery
//! - CLI executable path resolution
//! - System capability checks (Azure CLI, winget)
//!
//! These functions are pure utilities that don't depend on Tauri state
//! and can be easily tested in isolation.

use std::env;
use std::path::PathBuf;
use std::process::Command;

/// Get the path to the Azure CLI executable
pub fn get_azure_cli_path() -> (String, bool) {
    let program_files = env::var("ProgramFiles").unwrap_or_else(|_| "C:\\Program Files".to_string());
    let az_path = format!("{}\\Microsoft SDKs\\Azure\\CLI2\\wbin\\az.cmd", program_files);
    let az_path_buf = PathBuf::from(&az_path);
    let use_direct_path = az_path_buf.exists();
    (az_path, use_direct_path)
}

/// Get current Azure subscription ID from CLI
pub fn get_azure_subscription_id() -> Result<String, String> {
    let (az_path, use_direct_path) = get_azure_cli_path();
    
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

/// Check if Azure CLI is installed
pub fn check_azure_cli_installed() -> bool {
    let (az_path, use_direct_path) = get_azure_cli_path();
    if use_direct_path {
        PathBuf::from(&az_path).exists()
    } else {
        Command::new("az")
            .arg("--version")
            .output()
            .is_ok()
    }
}

/// Check if winget is available
pub fn check_winget_available() -> bool {
    Command::new("winget")
        .arg("--version")
        .output()
        .is_ok()
}

/// Find the repository root by looking for .git directory
pub fn find_repo_root() -> Result<PathBuf, String> {
    let current_dir = env::current_dir()
        .map_err(|e| format!("Failed to get current directory: {}", e))?;
    
    let mut search_dir = current_dir.clone();
    
    loop {
        let git_dir = search_dir.join(".git");
        if git_dir.exists() {
            return Ok(search_dir);
        }
        
        match search_dir.parent() {
            Some(parent) => search_dir = parent.to_path_buf(),
            None => return Err("Could not find repository root (.git directory)".to_string()),
        }
    }
}

/// Get the path to the built .NET CLI executable
pub fn get_cli_executable_path() -> Result<PathBuf, String> {
    let repo_root = find_repo_root()?;
    
    let expected_exe = repo_root
        .join("tools")
        .join("Mystira.DevHub.CLI")
        .join("bin")
        .join("Debug")
        .join("net9.0")
        .join("Mystira.DevHub.CLI.exe");
    
    let expected_dll = repo_root
        .join("tools")
        .join("Mystira.DevHub.CLI")
        .join("bin")
        .join("Debug")
        .join("net9.0")
        .join("Mystira.DevHub.CLI.dll");
    
    if expected_exe.exists() {
        Ok(expected_exe)
    } else if expected_dll.exists() {
        Ok(expected_dll)
    } else {
        Err(format!(
            "CLI executable not found at: {} or {}",
            expected_exe.display(),
            expected_dll.display()
        ))
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn test_get_azure_cli_path() {
        let (path, _use_direct) = get_azure_cli_path();
        // Path should be non-empty
        assert!(!path.is_empty());
        // Should contain az.cmd or reference Azure CLI
        assert!(path.contains("az") || path.contains("Azure"));
    }

    #[test]
    fn test_get_resource_group_name() {
        use crate::azure::deployment::helpers::get_resource_group_name;
        
        assert_eq!(get_resource_group_name("dev"), "dev-euw-rg-mystira-app");
        assert_eq!(get_resource_group_name("prod"), "prod-euw-rg-mystira-app");
        assert_eq!(get_resource_group_name("test"), "test-euw-rg-mystira-app");
    }

    #[test]
    fn test_get_deployment_path() {
        use crate::azure::deployment::helpers::get_deployment_path;
        
        let path = get_deployment_path("/repo", "dev");
        assert_eq!(path, "/repo/src/Mystira.App.Infrastructure.Azure/Deployment/dev");
        
        let path2 = get_deployment_path("C:\\repo", "prod");
        assert!(path2.contains("prod") && path2.contains("Deployment"));
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn test_get_azure_cli_path() {
        let (path, use_direct) = get_azure_cli_path();
        // Path should be non-empty
        assert!(!path.is_empty());
        // Should contain az.cmd
        assert!(path.contains("az.cmd") || path.contains("az"));
    }

    #[test]
    fn test_get_resource_group_name() {
        use crate::azure::deployment::helpers::get_resource_group_name;
        
        assert_eq!(get_resource_group_name("dev"), "dev-euw-rg-mystira-app");
        assert_eq!(get_resource_group_name("prod"), "prod-euw-rg-mystira-app");
        assert_eq!(get_resource_group_name("test"), "test-euw-rg-mystira-app");
    }

    #[test]
    fn test_get_deployment_path() {
        use crate::azure::deployment::helpers::get_deployment_path;
        
        let path = get_deployment_path("/repo", "dev");
        assert_eq!(path, "/repo/src/Mystira.App.Infrastructure.Azure/Deployment/dev");
    }
}
