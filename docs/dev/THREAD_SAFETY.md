# Thread Safety in Chat Bot Services

## Overview

The multi-platform chat bot services are designed to be thread-safe for concurrent access. This document explains the thread safety guarantees and patterns used.

## Thread-Safe Components

### Concurrent Collections

All services use `ConcurrentDictionary<TKey, TValue>` for shared state:

```csharp
// Discord - channel caching
private readonly ConcurrentDictionary<ulong, IMessageChannel> _channelCache = new();

// Teams - bidirectional mapping
private readonly ConcurrentDictionary<ulong, string> _idToKey = new();
private readonly ConcurrentDictionary<string, ConversationReference> _keyToRef = new();
private readonly ConcurrentDictionary<string, ulong> _keyToId = new();

// WhatsApp - conversation tracking
private readonly ConcurrentDictionary<ulong, string> _activeConversations = new();
```

### Atomic Operations

For simple flags and counters, use `Interlocked`:

```csharp
// Thread-safe stop flag
private int _stopListening = 0; // 0 = false, 1 = true

// Check atomically
if (Interlocked.CompareExchange(ref _stopListening, 0, 0) == 1)
    return;

// Set atomically
Interlocked.Exchange(ref _stopListening, 1);
```

### Double-Checked Locking

For ID generation with collision handling:

```csharp
private ulong GetOrCreateChannelId(string key)
{
    // Fast path - check without lock
    if (_keyToId.TryGetValue(key, out var existingId))
        return existingId;

    // Slow path - lock for creation
    lock (_idLock)
    {
        // Double-check inside lock
        if (_keyToId.TryGetValue(key, out existingId))
            return existingId;

        // Generate and store new ID
        var channelId = GenerateDeterministicId(key);
        _idToKey.TryAdd(channelId, key);
        _keyToId[key] = channelId;
        return channelId;
    }
}
```

### Semaphore Per-User Locking

For preventing duplicate operations per user:

```csharp
internal sealed class UserLockEntry
{
    public SemaphoreSlim Semaphore { get; } = new(1, 1);
    public DateTime LastAccess { get; set; } = DateTime.UtcNow;
    public int ActiveCount;
}

private static readonly ConcurrentDictionary<ulong, UserLockEntry> _userLocks = new();

// Acquire lock with tracking
var lockEntry = _userLocks.GetOrAdd(userId, _ => new UserLockEntry());
Interlocked.Increment(ref lockEntry.ActiveCount);

if (!await lockEntry.Semaphore.WaitAsync(TimeSpan.FromSeconds(5)))
{
    Interlocked.Decrement(ref lockEntry.ActiveCount);
    return; // Already processing
}

try
{
    // Protected operation
}
finally
{
    lockEntry.Semaphore.Release();
    Interlocked.Decrement(ref lockEntry.ActiveCount);
}
```

## Thread Safety Guarantees

### Guaranteed Thread-Safe

| Operation | Guarantee |
|-----------|-----------|
| `SendMessageAsync` | Multiple concurrent calls safe |
| `SendEmbedAsync` | Multiple concurrent calls safe |
| `ReplyToMessageAsync` | Multiple concurrent calls safe |
| `GetStatus()` | Always safe, returns snapshot |
| `AddOrUpdateConversationReference` | Safe for concurrent updates |

### Requires Care

| Operation | Notes |
|-----------|-------|
| `StartAsync/StopAsync` | Call once per service lifecycle |
| `Dispose` | Idempotent but call only once |
| Broadcast methods | Event handlers may run concurrently |

## Event Handler Safety

Discord event handlers run on thread pool threads:

```csharp
// Handler may be called concurrently
async Task OnMessageReceived(SocketMessage msg)
{
    // Use atomic check before processing
    if (Interlocked.CompareExchange(ref _stopListening, 0, 0) == 1)
        return;

    // Process message...

    // Atomic stop
    if (shouldStop)
        Interlocked.Exchange(ref _stopListening, 1);
}
```

## Memory Leak Prevention

### Cleanup Timers

For per-user locks that could accumulate:

```csharp
private static readonly Timer _cleanupTimer;
private static readonly TimeSpan CleanupInterval = TimeSpan.FromMinutes(5);
private static readonly TimeSpan LockIdleTimeout = TimeSpan.FromMinutes(10);

static TicketModule()
{
    _cleanupTimer = new Timer(CleanupIdleLocks, null, CleanupInterval, CleanupInterval);
}

private static void CleanupIdleLocks(object? state)
{
    var cutoff = DateTime.UtcNow - LockIdleTimeout;
    foreach (var kvp in _userLocks)
    {
        var entry = kvp.Value;
        // Only remove if not in use and idle
        if (Interlocked.CompareExchange(ref entry.ActiveCount, 0, 0) == 0 &&
            entry.LastAccess < cutoff)
        {
            if (_userLocks.TryRemove(kvp.Key, out var removed))
                removed.Semaphore.Dispose();
        }
    }
}
```

### Event Handler Cleanup

Always unsubscribe handlers in finally blocks:

```csharp
_client.MessageReceived += OnMessageReceived;
try
{
    // Wait for response or timeout
}
finally
{
    _client.MessageReceived -= OnMessageReceived;
}
```

## Best Practices

1. **Use `ConcurrentDictionary`** for all shared dictionaries
2. **Use `Interlocked`** for simple flags/counters, not `volatile`
3. **Double-check in locks** when checking then creating
4. **Clean up resources** in finally blocks
5. **Set `_isConnected = false`** in Dispose
6. **Track active operations** before cleanup

## Anti-Patterns to Avoid

```csharp
// BAD: Race condition
if (!_dict.ContainsKey(key))
    _dict[key] = value;

// GOOD: Atomic operation
_dict.TryAdd(key, value);

// BAD: Volatile for complex state
private volatile bool _stopListening;

// GOOD: Interlocked for thread-safe flag
private int _stopListening = 0;
Interlocked.CompareExchange(ref _stopListening, 0, 0) == 1

// BAD: Dispose without clearing connected state
public void Dispose()
{
    _dict.Clear();
    _disposed = true;
}

// GOOD: Clear state properly
public void Dispose()
{
    if (_disposed) return;
    _isConnected = false;
    _dict.Clear();
    _disposed = true;
    GC.SuppressFinalize(this);
}
```

## Testing Thread Safety

```csharp
[Fact]
public async Task ConcurrentSends_ShouldNotCorruptState()
{
    var service = CreateConnectedService();
    var tasks = Enumerable.Range(0, 100)
        .Select(i => service.SendMessageAsync(channelId, $"Message {i}"));

    await Task.WhenAll(tasks);

    // Verify no exceptions and correct state
}
```
