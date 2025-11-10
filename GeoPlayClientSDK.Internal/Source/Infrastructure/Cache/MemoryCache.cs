using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GeoPlayClientSDK.Internal.Infrastructure.Cache
{
    public class MemoryCache : ICache
    {
        private readonly Dictionary<string, CacheEntry> _cache = new Dictionary<string, CacheEntry>();
        private readonly object _lock = new object();

        private class CacheEntry
        {
            public object Value { get; set; }
            public DateTime? ExpiresAt { get; set; }
        }

        public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
        {
            lock (_lock)
            {
                if (expiration.HasValue)
                {
                    _cache[key] = new CacheEntry
                    {
                        Value = value,
                        ExpiresAt = DateTime.UtcNow.Add(expiration.Value)
                    };
                }
                else
                { 
                    _cache[key] = new CacheEntry
                    {
                        Value = value,
                        ExpiresAt = null
                    };
                }
            }
            return Task.CompletedTask;
        }

        public Task<T?> GetAsync<T>(string key)
        {
            lock (_lock)
            {
                if (!_cache.TryGetValue(key, out var entry))
                    return Task.FromResult<T?>(default);

                if (entry.ExpiresAt.HasValue && entry.ExpiresAt.Value <= DateTime.UtcNow)
                {
                    _cache.Remove(key);
                    return Task.FromResult<T?>(default);
                }

                return Task.FromResult((T?)entry.Value);
            }
        }

        public Task RemoveAsync(string key)
        {
            lock (_lock)
            {
                _cache.Remove(key);
            }
            return Task.CompletedTask;
        }

        public Task<bool> ExistsAsync(string key)
        {
            lock (_lock)
            {
                return Task.FromResult(_cache.ContainsKey(key));
            }
        }
    }
}