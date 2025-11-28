//! Caching layer with TTL support.
//!
//! This module provides in-memory caching for expensive operations like:
//! - Azure resource lists
//! - GitHub deployment history
//! - Service status checks
//!
//! Cache entries expire after their TTL, and can be manually invalidated.

use crate::config::get_config;
use std::collections::HashMap;
use std::sync::{Arc, Mutex};
use std::time::{Duration, SystemTime};
use tracing::{debug, trace};

/// Cache entry with TTL
#[derive(Clone)]
struct CacheEntry<T> {
    data: T,
    expires_at: SystemTime,
}

impl<T> CacheEntry<T> {
    fn new(data: T, ttl_seconds: u64) -> Self {
        let expires_at = SystemTime::now() + Duration::from_secs(ttl_seconds);
        CacheEntry { data, expires_at }
    }
    
    fn is_expired(&self) -> bool {
        SystemTime::now() > self.expires_at
    }
}

// Generic cache removed - using type-specific caches for simplicity and type safety

// Type-specific cache implementations for better type safety

/// String-based cache (for JSON responses, etc.)
pub struct StringCache {
    entries: Arc<Mutex<HashMap<String, CacheEntry<String>>>>,
}

impl StringCache {
    pub fn new() -> Self {
        StringCache {
            entries: Arc::new(Mutex::new(HashMap::new())),
        }
    }
    
    pub fn get(&self, key: &str) -> Option<String> {
        let config = get_config();
        if !config.cache.enabled {
            return None;
        }
        
        let mut entries = self.entries.lock().unwrap();
        
        // Clean up expired entries
        entries.retain(|_, entry| !entry.is_expired());
        
        let entry = entries.get(key)?;
        if entry.is_expired() {
            entries.remove(key);
            return None;
        }
        
        Some(entry.data.clone())
    }
    
    pub fn set(&self, key: String, value: String, ttl_seconds: u64) {
        let config = get_config();
        if !config.cache.enabled {
            return;
        }
        
        let key_clone = key.clone();
        let mut entries = self.entries.lock().unwrap();
        entries.insert(key, CacheEntry::new(value, ttl_seconds));
        trace!("Cache entry set: {} (TTL: {}s)", key_clone, ttl_seconds);
    }
    
    pub fn invalidate(&self, key: &str) {
        let mut entries = self.entries.lock().unwrap();
        if entries.remove(key).is_some() {
            debug!("Cache invalidated: {}", key);
        }
    }
    
    pub fn clear(&self) {
        let mut entries = self.entries.lock().unwrap();
        entries.clear();
        debug!("Cache cleared");
    }
}

impl Default for StringCache {
    fn default() -> Self {
        Self::new()
    }
}

// Global cache instances
lazy_static::lazy_static! {
    pub static ref AZURE_RESOURCES_CACHE: StringCache = StringCache::new();
    pub static ref GITHUB_DEPLOYMENTS_CACHE: StringCache = StringCache::new();
}

/// Get cache TTL for a specific operation type
pub fn get_cache_ttl(cache_type: &str) -> u64 {
    let config = get_config();
    match cache_type {
        "azure_resources" => config.cache.azure_resources_ttl,
        "github_deployments" => config.cache.github_deployments_ttl,
        _ => config.cache.default_ttl,
    }
}

