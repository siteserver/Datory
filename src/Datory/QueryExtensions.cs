using System.Linq;
using System.Reflection;
using Datory.Annotations;
using Datory.Caching;
using Microsoft.Extensions.Caching.Distributed;
using SqlKata;

namespace Datory
{
    /// <summary>
    /// https://stackoverflow.com/a/35273581
    /// </summary>
    public static class QueryExtensions
    {
        public static Query CachingGet(this Query query, string cacheKey, DistributedCacheEntryOptions options = null)
        {
            query.ClearComponent("cache").AddComponent("cache", new CachingCondition
            {
                Action = CachingAction.Get,
                CacheKey = cacheKey,
                Options = options
            });

            return query;
        }

        public static Query CachingSet(this Query query, string cacheKey, DistributedCacheEntryOptions options = null)
        {
            query.ClearComponent("cache").AddComponent("cache", new CachingCondition
            {
                Action = CachingAction.Set,
                CacheKey = cacheKey,
                Options = options
            });

            return query;
        }

        public static Query CachingRemove(this Query query, string cacheKey, DistributedCacheEntryOptions options = null)
        {
            query.ClearComponent("cache").AddComponent("cache", new CachingCondition
            {
                Action = CachingAction.Remove,
                CacheKey = cacheKey,
                Options = options
            });

            return query;
        }
    }
}
