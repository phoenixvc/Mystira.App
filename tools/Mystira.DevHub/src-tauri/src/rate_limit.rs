//! Rate limiting module.
//!
//! This module provides rate limiting functionality for API calls to prevent
//! hitting service limits (Azure API, GitHub API, etc.).

use crate::config::get_config;
use std::collections::HashMap;
use std::sync::{Arc, Mutex};
use std::time::{Duration, SystemTime, UNIX_EPOCH};
use tokio::time::sleep;
use tracing::debug;

/// Rate limiter for API calls
pub struct RateLimiter {
    // Map of service name to list of request timestamps
    requests: Arc<Mutex<HashMap<String, Vec<u64>>>>,
}

impl RateLimiter {
    pub fn new() -> Self {
        RateLimiter {
            requests: Arc::new(Mutex::new(HashMap::new())),
        }
    }
    
    /// Check if a request is allowed and wait if necessary
    pub async fn wait_if_needed(&self, service: &str, requests_per_minute: u32) {
        let config = get_config();
        if !config.rate_limit.enabled {
            return;
        }
        
        let now = SystemTime::now()
            .duration_since(UNIX_EPOCH)
            .unwrap()
            .as_secs();
        
        let minute_ago = now.saturating_sub(60);
        
        // Determine if we need to wait (lock scope ends before await)
        let wait_seconds = {
            let mut requests = self.requests.lock().unwrap();
            
            // Clean up old requests (older than 1 minute)
            if let Some(timestamps) = requests.get_mut(service) {
                timestamps.retain(|&ts| ts > minute_ago);
                
                // Check if we've hit the rate limit
                if timestamps.len() >= requests_per_minute as usize {
                    let oldest_request = timestamps.first().copied().unwrap_or(now);
                    let wait = 60 - (now - oldest_request);
                    if wait > 0 {
                        Some(wait)
                    } else {
                        None
                    }
                } else {
                    None
                }
            } else {
                None
            }
        }; // Lock is dropped here
        
        // Wait if needed (no lock held)
        if let Some(wait_secs) = wait_seconds {
            debug!("Rate limit reached for {}, waiting {} seconds", service, wait_secs);
            sleep(Duration::from_secs(wait_secs)).await;
        }
        
        // Record this request (re-acquire lock)
        let mut requests = self.requests.lock().unwrap();
        let now_after_wait = SystemTime::now()
            .duration_since(UNIX_EPOCH)
            .unwrap()
            .as_secs();
        let minute_ago_after_wait = now_after_wait.saturating_sub(60);
        
        if let Some(timestamps) = requests.get_mut(service) {
            timestamps.retain(|&ts| ts > minute_ago_after_wait);
            timestamps.push(now_after_wait);
        } else {
            // First request for this service
            requests.insert(service.to_string(), vec![now_after_wait]);
        }
    }
    
    /// Reset rate limiter for a service
    pub fn reset(&self, service: &str) {
        let mut requests = self.requests.lock().unwrap();
        requests.remove(service);
    }
    
    /// Reset all rate limiters
    pub fn reset_all(&self) {
        let mut requests = self.requests.lock().unwrap();
        requests.clear();
    }
}

impl Default for RateLimiter {
    fn default() -> Self {
        Self::new()
    }
}

// Global rate limiter instance
lazy_static::lazy_static! {
    pub static ref RATE_LIMITER: RateLimiter = RateLimiter::new();
}

/// Wait if rate limit is needed for Azure API calls
pub async fn wait_azure_rate_limit() {
    let config = get_config();
    RATE_LIMITER.wait_if_needed("azure", config.rate_limit.azure_requests_per_minute).await;
}

/// Wait if rate limit is needed for GitHub API calls
pub async fn wait_github_rate_limit() {
    let config = get_config();
    RATE_LIMITER.wait_if_needed("github", config.rate_limit.github_requests_per_minute).await;
}
