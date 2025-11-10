using System;
using System.Threading.Tasks;

namespace GeoPlayClientSDK.Internal.Infrastructure.Cache
{
    public interface ICache
    {
        Task SetAsync<T>(string key, T value, TimeSpan? expiration = null);
        Task<T?> GetAsync<T>(string key);
        Task RemoveAsync(string key);
        Task<bool> ExistsAsync(string key);
    }
}