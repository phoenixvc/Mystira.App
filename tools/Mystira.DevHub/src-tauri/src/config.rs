//! Configuration management module.
//!
//! This module provides centralized configuration management:
//! - Environment variable handling with defaults
//! - Config file support (JSON-based)
//! - Settings persistence
//! - Runtime configuration updates
//!
//! Configuration is loaded from:
//! 1. Environment variables (highest priority)
//! 2. Config file (`config.json` in app data directory)
//! 3. Default values (fallback)

use serde::{Deserialize, Serialize};
use std::env;
use std::path::PathBuf;
use std::fs;
use tracing::{info, debug};

/// Application configuration structure
#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct AppConfig {
    /// Azure configuration
    pub azure: AzureConfig,
    /// GitHub configuration
    pub github: GitHubConfig,
    /// Caching configuration
    pub cache: CacheConfig,
    /// Retry configuration
    pub retry: RetryConfig,
    /// Rate limiting configuration
    pub rate_limit: RateLimitConfig,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct AzureConfig {
    /// Default subscription ID
    pub default_subscription: Option<String>,
    /// Default resource group name pattern
    pub resource_group_pattern: Option<String>,
    /// Default location
    pub default_location: String,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct GitHubConfig {
    /// Default repository owner
    pub default_owner: Option<String>,
    /// Default repository name
    pub default_repo: Option<String>,
    /// API rate limit (requests per minute)
    pub api_rate_limit: u32,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct CacheConfig {
    /// Enable caching
    pub enabled: bool,
    /// Default TTL for cached data (seconds)
    pub default_ttl: u64,
    /// Azure resources cache TTL (seconds)
    pub azure_resources_ttl: u64,
    /// GitHub deployments cache TTL (seconds)
    pub github_deployments_ttl: u64,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct RetryConfig {
    /// Enable automatic retries
    pub enabled: bool,
    /// Maximum number of retries
    pub max_retries: u32,
    /// Initial backoff delay in milliseconds
    pub initial_backoff_ms: u64,
    /// Maximum backoff delay in milliseconds
    pub max_backoff_ms: u64,
    /// Backoff multiplier
    pub backoff_multiplier: f64,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct RateLimitConfig {
    /// Enable rate limiting
    pub enabled: bool,
    /// Azure API rate limit (requests per minute)
    pub azure_requests_per_minute: u32,
    /// GitHub API rate limit (requests per minute)
    pub github_requests_per_minute: u32,
}

impl Default for AppConfig {
    fn default() -> Self {
        AppConfig {
            azure: AzureConfig {
                default_subscription: None,
                resource_group_pattern: None,
                default_location: "westeurope".to_string(),
            },
            github: GitHubConfig {
                default_owner: None,
                default_repo: None,
                api_rate_limit: 60,
            },
            cache: CacheConfig {
                enabled: true,
                default_ttl: 300, // 5 minutes
                azure_resources_ttl: 300,
                github_deployments_ttl: 600, // 10 minutes
            },
            retry: RetryConfig {
                enabled: true,
                max_retries: 3,
                initial_backoff_ms: 100,
                max_backoff_ms: 5000,
                backoff_multiplier: 2.0,
            },
            rate_limit: RateLimitConfig {
                enabled: true,
                azure_requests_per_minute: 30,
                github_requests_per_minute: 60,
            },
        }
    }
}

impl AppConfig {
    /// Load configuration from environment variables and config file
    pub fn load() -> Self {
        debug!("Loading application configuration");
        
        // Start with defaults
        let mut config = AppConfig::default();
        
        // Override from environment variables
        config.load_from_env();
        
        // Override from config file if it exists
        if let Some(config_file) = Self::get_config_file_path() {
            if let Ok(file_config) = Self::load_from_file(&config_file) {
                info!("Loaded configuration from file: {:?}", config_file);
                config.merge(file_config);
            } else {
                debug!("No config file found at {:?}, using defaults", config_file);
            }
        }
        
        config
    }
    
    /// Load configuration from environment variables
    fn load_from_env(&mut self) {
        // Azure settings
        if let Ok(sub) = env::var("MYSTIRA_AZURE_SUBSCRIPTION") {
            self.azure.default_subscription = Some(sub);
        }
        if let Ok(loc) = env::var("MYSTIRA_AZURE_LOCATION") {
            self.azure.default_location = loc;
        }
        
        // GitHub settings
        if let Ok(owner) = env::var("MYSTIRA_GITHUB_OWNER") {
            self.github.default_owner = Some(owner);
        }
        if let Ok(repo) = env::var("MYSTIRA_GITHUB_REPO") {
            self.github.default_repo = Some(repo);
        }
        
        // Cache settings
        if let Ok(enabled) = env::var("MYSTIRA_CACHE_ENABLED") {
            self.cache.enabled = enabled.parse().unwrap_or(true);
        }
        if let Ok(ttl) = env::var("MYSTIRA_CACHE_TTL") {
            if let Ok(ttl_val) = ttl.parse::<u64>() {
                self.cache.default_ttl = ttl_val;
            }
        }
        
        // Retry settings
        if let Ok(enabled) = env::var("MYSTIRA_RETRY_ENABLED") {
            self.retry.enabled = enabled.parse().unwrap_or(true);
        }
        if let Ok(max) = env::var("MYSTIRA_RETRY_MAX") {
            if let Ok(max_val) = max.parse::<u32>() {
                self.retry.max_retries = max_val;
            }
        }
        
        // Rate limit settings
        if let Ok(enabled) = env::var("MYSTIRA_RATE_LIMIT_ENABLED") {
            self.rate_limit.enabled = enabled.parse().unwrap_or(true);
        }
    }
    
    /// Load configuration from a JSON file
    fn load_from_file(path: &PathBuf) -> Result<AppConfig, String> {
        let content = fs::read_to_string(path)
            .map_err(|e| format!("Failed to read config file: {}", e))?;
        
        let config: AppConfig = serde_json::from_str(&content)
            .map_err(|e| format!("Failed to parse config file: {}", e))?;
        
        Ok(config)
    }
    
    /// Merge another configuration into this one (other takes precedence)
    fn merge(&mut self, other: AppConfig) {
        // Merge Azure config
        if other.azure.default_subscription.is_some() {
            self.azure.default_subscription = other.azure.default_subscription;
        }
        if !other.azure.default_location.is_empty() {
            self.azure.default_location = other.azure.default_location;
        }
        if other.azure.resource_group_pattern.is_some() {
            self.azure.resource_group_pattern = other.azure.resource_group_pattern;
        }
        
        // Merge GitHub config
        if other.github.default_owner.is_some() {
            self.github.default_owner = other.github.default_owner;
        }
        if other.github.default_repo.is_some() {
            self.github.default_repo = other.github.default_repo;
        }
        self.github.api_rate_limit = other.github.api_rate_limit;
        
        // Merge cache config
        self.cache = other.cache;
        
        // Merge retry config
        self.retry = other.retry;
        
        // Merge rate limit config
        self.rate_limit = other.rate_limit;
    }
    
    /// Save configuration to file
    pub fn save(&self) -> Result<(), String> {
        let config_file = Self::get_config_file_path()
            .ok_or_else(|| "Could not determine config file path".to_string())?;
        
        // Ensure directory exists
        if let Some(parent) = config_file.parent() {
            fs::create_dir_all(parent)
                .map_err(|e| format!("Failed to create config directory: {}", e))?;
        }
        
        let json = serde_json::to_string_pretty(self)
            .map_err(|e| format!("Failed to serialize config: {}", e))?;
        
        fs::write(&config_file, json)
            .map_err(|e| format!("Failed to write config file: {}", e))?;
        
        info!("Configuration saved to {:?}", config_file);
        Ok(())
    }
    
    /// Get the path to the configuration file
    fn get_config_file_path() -> Option<PathBuf> {
        // Try to get app data directory from Tauri or use a default location
        if let Ok(app_data) = env::var("APPDATA") {
            // Windows: %APPDATA%\MystiraDevHub\config.json
            Some(PathBuf::from(app_data).join("MystiraDevHub").join("config.json"))
        } else if let Ok(home) = env::var("HOME") {
            // Unix-like: ~/.config/mystira-devhub/config.json
            Some(PathBuf::from(home).join(".config").join("mystira-devhub").join("config.json"))
        } else {
            None
        }
    }
    
    /// Get an environment variable with a default value
    #[allow(dead_code)]
    pub fn env_var_or(key: &str, default: &str) -> String {
        env::var(key).unwrap_or_else(|_| default.to_string())
    }
    
    /// Get an optional environment variable
    #[allow(dead_code)]
    pub fn env_var_opt(key: &str) -> Option<String> {
        env::var(key).ok()
    }
}

/// Get the application configuration (singleton pattern via Tauri state)
pub fn get_config() -> AppConfig {
    // For now, load fresh each time. In production, this could be cached in Tauri state
    AppConfig::load()
}

/// Get current application configuration
#[tauri::command]
pub fn get_app_config() -> Result<AppConfig, String> {
    Ok(get_config())
}

/// Save application configuration
#[tauri::command]
pub fn save_app_config(config: AppConfig) -> Result<(), String> {
    config.save()
}

/// Reload configuration from file and environment
#[tauri::command]
pub fn reload_config() -> Result<AppConfig, String> {
    Ok(AppConfig::load())
}

