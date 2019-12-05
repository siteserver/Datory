using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Datory.Utils;
using Microsoft.Extensions.Caching.Distributed;

namespace Datory.Extensions
{
    public static class DistributedCachingExtensions
    {
        public static async Task<T> GetOrCreateAsync<T>(this IDistributedCache distributedCache, string key, Func<Task<T>> factory, DistributedCacheEntryOptions options)
        {
            var value = await distributedCache.GetAsync<T>(key);
            if (value != null) return value;

            value = await factory();
            if (value != null)
            {
                await distributedCache.SetAsync<T>(key, value, options);
            }

            return value;
        }

        public static async Task<int> GetOrCreateIntAsync(this IDistributedCache distributedCache, string key, Func<Task<int>> factory, DistributedCacheEntryOptions options)
        {
            var value = await distributedCache.GetStringAsync(key);
            if (value != null) return Utilities.ToInt(value);

            var intValue = await factory();
            await distributedCache.SetStringAsync(key, intValue.ToString(), options);

            return intValue;
        }

        public static async Task<bool> GetOrCreateBoolAsync(this IDistributedCache distributedCache, string key, Func<Task<bool>> factory, DistributedCacheEntryOptions options)
        {
            var value = await distributedCache.GetStringAsync(key);
            if (value != null) return Utilities.ToBool(value);

            var boolValue = await factory();
            await distributedCache.SetAsync<bool>(key, boolValue, options);

            return boolValue;
        }

        public static async Task SetAsync<T>(this IDistributedCache distributedCache, string key, T value)
        {
            if (value != null)
            {
                var data = Encoding.UTF8.GetBytes(Utilities.JsonSerialize(value));
                await distributedCache.SetAsync(key, data, new DistributedCacheEntryOptions());
            }
        }

        public static async Task SetAsync<T>(this IDistributedCache distributedCache, string key, T value, DistributedCacheEntryOptions options, CancellationToken token = default)
        {
            if (value != null)
            {
                var data = Encoding.UTF8.GetBytes(Utilities.JsonSerialize(value));
                await distributedCache.SetAsync(key, data, options, token);
            }
        }

        public static async Task<T> GetAsync<T>(this IDistributedCache distributedCache, string key, CancellationToken token = default)
        {
            var result = await distributedCache.GetAsync(key, token);
            if (result != null)
            {
                var data = Encoding.UTF8.GetString(result);
                return Utilities.JsonDeserialize<T>(data);
            }
            return default;
        }
    }
}
