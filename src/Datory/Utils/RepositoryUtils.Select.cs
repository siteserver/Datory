using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Dapper;
using Datory.Caching;
using Microsoft.Extensions.Caching.Distributed;
using SqlKata;

[assembly: InternalsVisibleTo("Datory.Tests")]

namespace Datory.Utils
{
    internal static partial class RepositoryUtils
    {
        public static async Task<bool> ExistsAsync(IDistributedCache cache, IDatabase database, string tableName,
            Query query = null)
        {
            var xQuery = NewQuery(tableName, query);
            xQuery.ClearComponent("select").SelectRaw("COUNT(1)").ClearComponent("order");
            var compileInfo = await CompileAsync(cache, database, tableName, xQuery);

            if (compileInfo.Caching != null && compileInfo.Caching.Action == CachingAction.Get)
            {
                return await cache.GetOrCreateAsync(compileInfo.Caching.CacheKey,
                    async () => await _ExistsAsync(database, compileInfo),
                    compileInfo.Caching.Options
                );
            }

            return await _ExistsAsync(database, compileInfo);
        }

        private static async Task<bool> _ExistsAsync(IDatabase database, CompileInfo compileInfo)
        {
            using var connection = database.GetConnection();
            return await connection.ExecuteScalarAsync<bool>(compileInfo.Sql, compileInfo.NamedBindings);
        }

        public static async Task<int> CountAsync(IDistributedCache cache, IDatabase database, string tableName, Query query = null)
        {
            var xQuery = NewQuery(tableName, query);
            xQuery.ClearComponent("order").AsCount();
            var compileInfo = await CompileAsync(cache, database, tableName, xQuery);

            if (compileInfo.Caching != null && compileInfo.Caching.Action == CachingAction.Get)
            {
                return await cache.GetOrCreateAsync(compileInfo.Caching.CacheKey,
                    async () => await _CountAsync(database, compileInfo),
                    compileInfo.Caching.Options
                );
            }

            return await _CountAsync(database, compileInfo);
        }

        private static async Task<int> _CountAsync(IDatabase database, CompileInfo compileInfo)
        {
            using var connection = database.GetConnection();
            return await connection.ExecuteScalarAsync<int>(compileInfo.Sql, compileInfo.NamedBindings);
        }

        public static async Task<int> SumAsync(IDistributedCache cache, IDatabase database, string tableName, string columnName, Query query = null)
        {
            var xQuery = NewQuery(tableName, query);
            xQuery.AsSum(columnName);
            var compileInfo = await CompileAsync(cache, database, tableName, xQuery);

            if (compileInfo.Caching != null && compileInfo.Caching.Action == CachingAction.Get)
            {
                return await cache.GetOrCreateAsync(compileInfo.Caching.CacheKey,
                    async () => await _SumAsync(database, compileInfo),
                    compileInfo.Caching.Options
                );
            }

            return await _SumAsync(database, compileInfo);
        }

        private static async Task<int> _SumAsync(IDatabase database, CompileInfo compileInfo)
        {
            using var connection = database.GetConnection();
            return await connection.ExecuteScalarAsync<int>(compileInfo.Sql, compileInfo.NamedBindings);
        }

        public static async Task<TValue> GetValueAsync<TValue>(IDistributedCache cache, IDatabase database, string tableName, Query query)
        {
            if (query == null) return default;

            var xQuery = NewQuery(tableName, query);
            xQuery.Limit(1);
            var compileInfo = await CompileAsync(cache, database, tableName, xQuery);

            if (compileInfo.Caching != null && compileInfo.Caching.Action == CachingAction.Get)
            {
                return await cache.GetOrCreateAsync(compileInfo.Caching.CacheKey,
                    async () => await _GetValueAsync<TValue>(database, compileInfo),
                    compileInfo.Caching.Options
                );
            }

            return await _GetValueAsync<TValue>(database, compileInfo);
        }

        private static async Task<TValue> _GetValueAsync<TValue>(IDatabase database, CompileInfo compileInfo)
        {
            using var connection = database.GetConnection();
            return await connection.QueryFirstOrDefaultAsync<TValue>(compileInfo.Sql, compileInfo.NamedBindings);
        }

        public static async Task<IEnumerable<TValue>> GetValueListAsync<TValue>(IDistributedCache cache, IDatabase database, string tableName, Query query = null)
        {
            var xQuery = NewQuery(tableName, query);
            var compileInfo = await CompileAsync(cache, database, tableName, xQuery);

            if (compileInfo.Caching != null && compileInfo.Caching.Action == CachingAction.Get)
            {
                return await cache.GetOrCreateAsync(compileInfo.Caching.CacheKey,
                    async () => await _GetValueListAsync<TValue>(database, compileInfo),
                    compileInfo.Caching.Options
                );
            }

            return await _GetValueListAsync<TValue>(database, compileInfo);
        }

        private static async Task<IEnumerable<TValue>> _GetValueListAsync<TValue>(IDatabase database, CompileInfo compileInfo)
        {
            using var connection = database.GetConnection();
            return await connection.QueryAsync<TValue>(compileInfo.Sql, compileInfo.NamedBindings);
        }

        public static async Task<int> MaxAsync(IDistributedCache cache, IDatabase database, string tableName, string columnName, Query query = null)
        {
            var xQuery = NewQuery(tableName, query);
            xQuery.AsMax(columnName);
            var compileInfo = await CompileAsync(cache, database, tableName, xQuery);

            if (compileInfo.Caching != null && compileInfo.Caching.Action == CachingAction.Get)
            {
                return await cache.GetOrCreateAsync(compileInfo.Caching.CacheKey,
                    async () => await _MaxAsync(database, compileInfo),
                    compileInfo.Caching.Options
                );
            }

            return await _MaxAsync(database, compileInfo);
        }

        private static async Task<int> _MaxAsync(IDatabase database, CompileInfo compileInfo)
        {
            using var connection = database.GetConnection();
            var max = await connection.QueryFirstOrDefaultAsync<int?>(compileInfo.Sql, compileInfo.NamedBindings);
            return max ?? 0;
        }

        public static async Task<T> GetObjectAsync<T>(IDistributedCache cache, IDatabase database, string tableName, Query query = null) where T : Entity
        {
            var xQuery = NewQuery(tableName, query);
            xQuery.ClearComponent("select").SelectRaw("*").Limit(1);
            var compileInfo = await CompileAsync(cache, database, tableName, xQuery);

            if (compileInfo.Caching != null && compileInfo.Caching.Action == CachingAction.Get)
            {
                return await cache.GetOrCreateAsync(compileInfo.Caching.CacheKey,
                    async () => await _GetObjectAsync<T>(cache, database, tableName, compileInfo),
                    compileInfo.Caching.Options
                );
            }

            return await _GetObjectAsync<T>(cache, database, tableName, compileInfo);
        }

        private static async Task<T> _GetObjectAsync<T>(IDistributedCache cache, IDatabase database, string tableName, CompileInfo compileInfo) where T : Entity
        {
            T value;
            using (var connection = database.GetConnection())
            {
                value = await connection.QueryFirstOrDefaultAsync<T>(compileInfo.Sql, compileInfo.NamedBindings);
            }

            await SyncAndCheckGuidAsync(cache, database, tableName, value);
            return value;
        }

        public static async Task<IEnumerable<T>> GetObjectListAsync<T>(IDistributedCache cache, IDatabase database, string tableName, Query query = null) where T : Entity
        {
            var xQuery = NewQuery(tableName, query);
            xQuery.ClearComponent("select").SelectRaw("*");
            var compileInfo = await CompileAsync(cache, database, tableName, xQuery);

            if (compileInfo.Caching != null && compileInfo.Caching.Action == CachingAction.Get)
            {
                return await cache.GetOrCreateAsync(compileInfo.Caching.CacheKey,
                    async () => await _GetObjectListAsync<T>(cache, database, tableName, compileInfo),
                    compileInfo.Caching.Options
                );
            }

            return await _GetObjectListAsync<T>(cache, database, tableName, compileInfo);
        }

        private static async Task<IEnumerable<T>> _GetObjectListAsync<T>(IDistributedCache cache, IDatabase database, string tableName, CompileInfo compileInfo) where T : Entity
        {
            IEnumerable<T> values;
            using (var connection = database.GetConnection())
            {
                values = await connection.QueryAsync<T>(compileInfo.Sql, compileInfo.NamedBindings);
            }

            foreach (var dataInfo in values)
            {
                await SyncAndCheckGuidAsync(cache, database, tableName, dataInfo);
            }
            return values;
        }
    }
}
