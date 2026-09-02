//using Microsoft.Extensions.Caching.Distributed;
//using StackExchange.Redis;
//using System.Collections.Concurrent;
//using System.Text.Json;

//namespace FullText.API.Extensions;

//public static class DistributedCacheExtensions
//{
//    private static readonly ConcurrentDictionary<string, RefCountedSemaphore> _semaphores = new();

//    private static readonly DistributedCacheEntryOptions _defaultOptions = new()
//    {
//        AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30)
//    };

//    /// <summary>
//    /// Gets a cached value or creates it via <paramref name="factory"/> if missing, guarding
//    /// against cache stampede with a per-key lock.
//    /// </summary>
//    /// <remarks>
//    /// - A cache-hit that fails to deserialize (corrupt/incompatible JSON) is treated as a miss:
//    ///   the bad entry is removed and the factory is invoked.
//    /// - A factory result of <c>null</c> is never cached, so repeated calls for a key whose
//    ///   factory legitimately returns null will always re-invoke the factory. If your factory
//    ///   can return null as a valid, common result and you want that cached too, this needs a
//    ///   null-sentinel wrapper — ask if you want that added.
//    /// - If the per-key lock isn't acquired within 2s, this throws <see cref="TimeoutException"/>
//    ///   rather than silently returning default(T), so callers can't mistake "timed out" for
//    ///   "legitimately cached null".
//    /// </remarks>
//    public static async Task<T?> GetOrCreateAsync<T>(
//        this IDistributedCache cache,
//        string key,
//        Func<CancellationToken, ValueTask<T>> factory,
//        DistributedCacheEntryOptions? distributedCacheEntryOptions = null,
//        CancellationToken cancellationToken = default)
//    {
//        if (TryDeserialize<T>(await cache.GetStringAsync(key, cancellationToken), out var hit))
//        {
//            return hit;
//        }

//        // This might produce a cache stampede.
//        // There are many solutions for this:
//        // - For a single-process application -> Semaphore
//        // - For a distributed application -> You can use redis distributed locks
//        // But for my case a per-key semaphore is enough.
//        var entry = _semaphores.AddOrUpdate(
//            key,
//            _ => new RefCountedSemaphore(),
//            (_, existing) =>
//            {
//                Interlocked.Increment(ref existing.RefCount);
//                return existing;
//            });

//        try
//        {
//            if (!await entry.Semaphore.WaitAsync(2000, cancellationToken))
//            {
//                throw new TimeoutException($"Timed out waiting for cache lock on key '{key}'.");
//            }

//            // Check for cache hit again (another request might have populated it while we waited)
//            if (TryDeserialize<T>(await cache.GetStringAsync(key, cancellationToken), out var hitAfterWait))
//            {
//                return hitAfterWait;
//            }

//            // 100% it's a cache miss
//            var response = await factory(cancellationToken);
//            if (response is not null)
//            {
//                await cache.SetStringAsync(
//                    key,
//                    JsonSerializer.Serialize(response),
//                    distributedCacheEntryOptions ?? _defaultOptions,
//                    cancellationToken);
//            }

//            return response;
//        }
//        finally
//        {
//            entry.Semaphore.Release();

//            if (Interlocked.Decrement(ref entry.RefCount) == 0)
//            {
//                // Only remove if nobody grabbed a reference to this exact entry in the meantime.
//                _semaphores.TryRemove(new KeyValuePair<string, RefCountedSemaphore>(key, entry));
//            }
//        }

//        // Local helper: attempts to deserialize a cached string, self-healing on corrupt data
//        // by removing the bad entry so the caller falls through to the factory path.
//        bool TryDeserialize<TValue>(string? cached, out TValue? value)
//        {
//            value = default;

//            if (string.IsNullOrWhiteSpace(cached))
//            {
//                return false;
//            }

//            try
//            {
//                value = JsonSerializer.Deserialize<TValue>(cached);
//            }
//            catch (JsonException)
//            {
//                value = default;
//            }

//            if (value is null)
//            {
//                // Fire-and-forget-but-awaited removal of the null/corrupt entry.
//                // (Deliberately not passed cancellationToken so an already-cancelled
//                // caller still cleans up bad cache data.)
//                _ = cache.RemoveAsync(key, CancellationToken.None);
//                return false;
//            }

//            return true;
//        }
//    }

//    private sealed class RefCountedSemaphore
//    {
//        public SemaphoreSlim Semaphore { get; } = new(1, 1);
//        public int RefCount = 1;
//    }
//}

using Microsoft.Extensions.Caching.Distributed;
using StackExchange.Redis;
using System.Text.Json;

namespace FullText.API.Extensions;

public static class DistributedCacheExtensions
{
    private static readonly DistributedCacheEntryOptions _defaultOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30)
    };

    // Only deletes the lock if it still holds OUR token — prevents releasing
    // a lock that expired and was re-acquired by someone else in the meantime.
    private const string ReleaseLockScript = """
        if redis.call("get", KEYS[1]) == ARGV[1] then
            return redis.call("del", KEYS[1])
        else
            return 0
        end
        """;

    /// <summary>
    /// Gets a cached value or creates it via <paramref name="factory"/> if missing, guarding
    /// against cache stampede across MULTIPLE application instances using a Redis-backed lock.
    /// </summary>
    /// <remarks>
    /// - <paramref name="redis"/> should be the same Redis instance your <see cref="IDistributedCache"/>
    ///   is backed by (or at least reachable from every app instance).
    /// - <paramref name="lockTtl"/> must comfortably exceed how long <paramref name="factory"/> takes.
    ///   If it's too short, another instance can acquire the lock while you're still working,
    ///   defeating the point. Default is 10s; tune per use case.
    /// - If the lock can't be acquired within <paramref name="acquireTimeout"/>, this method
    ///   falls back to just calling the factory directly (no lock) rather than throwing, since
    ///   in a distributed setting a slow/unavailable Redis shouldn't take your endpoint down.
    ///   Swap for a thrown exception if you'd rather fail loudly.
    /// </remarks>
    public static async Task<T?> GetOrCreateAsync<T>(
        this IDistributedCache cache,
        IConnectionMultiplexer redis,
        string key,
        Func<CancellationToken, ValueTask<T>> factory,
        DistributedCacheEntryOptions? distributedCacheEntryOptions = null,
        TimeSpan? lockTtl = null,
        TimeSpan? acquireTimeout = null,
        CancellationToken cancellationToken = default)
    {
        if (TryDeserialize<T>(await cache.GetStringAsync(key, cancellationToken), out var hit))
        {
            return hit;
        }

        var db = redis.GetDatabase();
        var lockKey = $"lock:{key}";
        var token = Guid.NewGuid().ToString("N"); // unique to this acquisition attempt
        var ttl = lockTtl ?? TimeSpan.FromSeconds(10);
        var timeout = acquireTimeout ?? TimeSpan.FromSeconds(2);

        bool acquired = await TryAcquireLockAsync(db, lockKey, token, ttl, timeout, cancellationToken);

        if (!acquired)
        {
            // Couldn't get the lock in time. Either:
            //   (a) someone else is populating the cache right now — briefly recheck cache, or
            //   (b) Redis is having a bad day.
            // We recheck once, and if still empty, just call the factory unprotected rather
            // than blocking the request indefinitely or throwing.
            if (TryDeserialize<T>(await cache.GetStringAsync(key, cancellationToken), out var lateHit))
            {
                return lateHit;
            }

            return await factory(cancellationToken);
        }

        try
        {
            // Check again — another instance may have populated the cache while we waited.
            if (TryDeserialize<T>(await cache.GetStringAsync(key, cancellationToken), out var hitAfterLock))
            {
                return hitAfterLock;
            }

            var response = await factory(cancellationToken);
            if (response is not null)
            {
                await cache.SetStringAsync(
                    key,
                    JsonSerializer.Serialize(response),
                    distributedCacheEntryOptions ?? _defaultOptions,
                    cancellationToken);
            }

            return response;
        }
        finally
        {
            // Best-effort release; if this fails, the lock simply expires via its TTL.
            try
            {
                await db.ScriptEvaluateAsync(ReleaseLockScript, [lockKey], [token]);
            }
            catch
            {
                // Swallow — TTL is the safety net.
            }
        }

        bool TryDeserialize<TValue>(string? cached, out TValue? value)
        {
            value = default;

            if (string.IsNullOrWhiteSpace(cached))
            {
                return false;
            }

            try
            {
                value = JsonSerializer.Deserialize<TValue>(cached);
            }
            catch (JsonException)
            {
                value = default;
            }

            if (value is null)
            {
                _ = cache.RemoveAsync(key, CancellationToken.None);
                return false;
            }

            return true;
        }
    }

    private static async Task<bool> TryAcquireLockAsync(
        IDatabase db,
        string lockKey,
        string token,
        TimeSpan ttl,
        TimeSpan acquireTimeout,
        CancellationToken cancellationToken)
    {
        var deadline = DateTime.UtcNow + acquireTimeout;
        var rng = Random.Shared;

        while (DateTime.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();

            bool acquired = await db.StringSetAsync(lockKey, token, ttl, When.NotExists);
            if (acquired)
            {
                return true;
            }

            // Jittered backoff so many waiting instances don't hammer Redis in lockstep.
            await Task.Delay(rng.Next(25, 75), cancellationToken);
        }

        return false;
    }
}