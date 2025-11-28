//! Retry logic with exponential backoff.
//!
//! This module provides automatic retry functionality for transient failures
//! with configurable exponential backoff strategies.

use crate::config::get_config;
use std::time::Duration;
use tracing::{debug, warn, error};
use tokio::time::sleep;

/// Retry policy configuration
#[derive(Debug, Clone)]
pub struct RetryPolicy {
    pub max_retries: u32,
    pub initial_backoff_ms: u64,
    pub max_backoff_ms: u64,
    pub backoff_multiplier: f64,
}

impl Default for RetryPolicy {
    fn default() -> Self {
        let config = get_config();
        RetryPolicy {
            max_retries: config.retry.max_retries,
            initial_backoff_ms: config.retry.initial_backoff_ms,
            max_backoff_ms: config.retry.max_backoff_ms,
            backoff_multiplier: config.retry.backoff_multiplier,
        }
    }
}

/// Execute a function with retry logic
pub async fn retry_with_backoff<F, Fut, T, E>(
    operation: F,
    policy: Option<RetryPolicy>,
) -> Result<T, E>
where
    F: Fn() -> Fut,
    Fut: std::future::Future<Output = Result<T, E>>,
{
    let policy = policy.unwrap_or_default();
    let config = get_config();
    
    if !config.retry.enabled {
        // If retries are disabled, just execute once
        return operation().await;
    }
    
    let mut attempt = 0;
    let mut backoff_ms = policy.initial_backoff_ms;
    
    loop {
        match operation().await {
            Ok(result) => {
                if attempt > 0 {
                    debug!("Operation succeeded after {} retries", attempt);
                }
                return Ok(result);
            }
            Err(e) => {
                if attempt >= policy.max_retries {
                    error!("Operation failed after {} retries", attempt);
                    return Err(e);
                }
                
                attempt += 1;
                warn!("Operation failed (attempt {}/{}), retrying in {}ms...", 
                    attempt, policy.max_retries + 1, backoff_ms);
                
                // Wait before retrying
                sleep(Duration::from_millis(backoff_ms)).await;
                
                // Calculate next backoff (exponential with cap)
                backoff_ms = ((backoff_ms as f64) * policy.backoff_multiplier) as u64;
                backoff_ms = backoff_ms.min(policy.max_backoff_ms);
            }
        }
    }
}

/// Check if an error is retryable (transient)
pub fn is_retryable_error(error_msg: &str) -> bool {
    let retryable_patterns = [
        "network",
        "timeout",
        "temporarily",
        "rate limit",
        "503",
        "502",
        "500",
        "connection",
        "refused",
    ];
    
    let error_lower = error_msg.to_lowercase();
    retryable_patterns.iter().any(|pattern| error_lower.contains(pattern))
}

/// Execute with retry, but only retry on retryable errors
pub async fn retry_on_retryable_error<F, Fut, T>(
    operation: F,
    policy: Option<RetryPolicy>,
) -> Result<T, String>
where
    F: Fn() -> Fut,
    Fut: std::future::Future<Output = Result<T, String>>,
{
    let policy = policy.unwrap_or_default();
    let config = get_config();
    
    if !config.retry.enabled {
        return operation().await;
    }
    
    let mut attempt = 0;
    let mut backoff_ms = policy.initial_backoff_ms;
    
    loop {
        match operation().await {
            Ok(result) => {
                if attempt > 0 {
                    debug!("Operation succeeded after {} retries", attempt);
                }
                return Ok(result);
            }
            Err(e) => {
                // Check if error is retryable
                if !is_retryable_error(&e) {
                    // Not retryable, return immediately
                    return Err(e);
                }
                
                if attempt >= policy.max_retries {
                    error!("Operation failed after {} retries: {}", attempt, e);
                    return Err(e);
                }
                
                attempt += 1;
                warn!("Retryable error (attempt {}/{}): {}, retrying in {}ms...", 
                    attempt, policy.max_retries + 1, e, backoff_ms);
                
                sleep(Duration::from_millis(backoff_ms)).await;
                
                // Exponential backoff
                backoff_ms = ((backoff_ms as f64) * policy.backoff_multiplier) as u64;
                backoff_ms = backoff_ms.min(policy.max_backoff_ms);
            }
        }
    }
}

