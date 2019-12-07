using System;
using System.Text;
using System.Threading.Tasks;
using Datory.Utils;
using Microsoft.Extensions.Caching.Distributed;

namespace Datory.Caching
{
    public static class CachingUtils
    {
        public static async Task<T> GetOrCreateAsync<T>(this IDistributedCache distributedCache, string key, Func<Task<T>> factory, DistributedCacheEntryOptions options = null)
        {
            var value = await distributedCache.GetAsync<T>(key);
            if (value != null) return value;

            value = await factory.Invoke();
            await SetAsync(distributedCache, key, value, options);

            return value;
        }

        public static async Task SetAsync<T>(this IDistributedCache distributedCache, string key, T value, DistributedCacheEntryOptions options = null)
        {
            if (value != null)
            {
                if (options == null)
                {
                    options = new DistributedCacheEntryOptions();
                }

                if (IsSimple(typeof(T)))
                {
                    var data = Encoding.UTF8.GetBytes(value.ToString());
                    await distributedCache.SetAsync(key, data, options);
                }
                else
                {
                    var data = Encoding.UTF8.GetBytes(Utilities.JsonSerialize(value));
                    await distributedCache.SetAsync(key, data, options);
                }
            }
        }

        public static async Task<T> GetAsync<T>(this IDistributedCache distributedCache, string key)
        {
            var result = await distributedCache.GetAsync(key);
            if (result == null) return default;

            if (IsSimple(typeof(T)))
            {
                var data = Encoding.UTF8.GetString(result);
                return Get<T>(data);
            }
            else
            {
                var data = Encoding.UTF8.GetString(result);
                return Utilities.JsonDeserialize<T>(data);
                    
            }
        }

        private static T Get<T>(object value, T defaultValue = default(T))
        {
            switch (value)
            {
                case null:
                    return defaultValue;
                case T variable:
                    return variable;
                default:
                    try
                    {
                        return (T)Convert.ChangeType(value, typeof(T));
                    }
                    catch (InvalidCastException)
                    {
                        return defaultValue;
                    }
            }
        }

        private static bool IsSimple(Type type)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                // nullable type, check if the nested type is simple.
                return IsSimple(type.GetGenericArguments()[0]);
            }
            return type.IsPrimitive
                   || type.IsEnum
                   || type == typeof(string)
                   || type == typeof(decimal);
        }
    }
}
