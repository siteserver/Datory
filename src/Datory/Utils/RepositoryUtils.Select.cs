using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Dapper;
using Datory.Extensions;
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
            var caching = await GetCachingAsync(xQuery, cache);
            if (caching != null)
            {
                return await cache.GetOrCreateBoolAsync(caching.Key,
                    async () => await _ExistsAsync(database, tableName, xQuery),
                    caching.Options
                );
            }

            return await _ExistsAsync(database, tableName, xQuery);
        }

        private static async Task<bool> _ExistsAsync(IDatabase database, string tableName, Query xQuery)
        {
            xQuery.ClearComponent("select").SelectRaw("COUNT(1)").ClearComponent("order");
            var (sql, bindings) = Compile(database, tableName, xQuery);

            using var connection = database.GetConnection();
            return await connection.ExecuteScalarAsync<bool>(sql, bindings);
        }

        public static async Task<int> CountAsync(IDistributedCache cache, IDatabase database, string tableName, Query query = null)
        {
            var xQuery = NewQuery(tableName, query);
            var caching = await GetCachingAsync(xQuery, cache);
            if (caching != null)
            {
                return await cache.GetOrCreateIntAsync(caching.Key,
                    async () => await _CountAsync(database, tableName, xQuery),
                    caching.Options
                );
            }

            return await _CountAsync(database, tableName, xQuery);
        }

        private static async Task<int> _CountAsync(IDatabase database, string tableName, Query xQuery)
        {
            xQuery.ClearComponent("order").AsCount();
            var (sql, bindings) = Compile(database, tableName, xQuery);

            using var connection = database.GetConnection();
            return await connection.ExecuteScalarAsync<int>(sql, bindings);
        }

        public static async Task<int> SumAsync(IDistributedCache cache, IDatabase database, string tableName, string columnName, Query query = null)
        {
            var xQuery = NewQuery(tableName, query);
            var caching = await GetCachingAsync(xQuery, cache);
            if (caching != null)
            {
                return await cache.GetOrCreateIntAsync(caching.Key,
                    async () => await _SumAsync(database, tableName, columnName, xQuery),
                    caching.Options
                );
            }

            return await _SumAsync(database, tableName, columnName, xQuery);
        }

        private static async Task<int> _SumAsync(IDatabase database, string tableName, string columnName, Query xQuery)
        {
            xQuery.AsSum(columnName);
            var (sql, bindings) = Compile(database, tableName, xQuery);

            using var connection = database.GetConnection();
            return await connection.ExecuteScalarAsync<int>(sql, bindings);
        }

        public static async Task<TValue> GetValueAsync<TValue>(IDistributedCache cache, IDatabase database, string tableName, Query query)
        {
            if (query == null) return default;

            var xQuery = NewQuery(tableName, query);
            var caching = await GetCachingAsync(xQuery, cache);
            if (caching != null)
            {
                return await cache.GetOrCreateAsync(caching.Key,
                    async () => await _GetValueAsync<TValue>(database, tableName, xQuery),
                    caching.Options
                );
            }

            return await _GetValueAsync<TValue>(database, tableName, xQuery);
        }

        private static async Task<TValue> _GetValueAsync<TValue>(IDatabase database, string tableName, Query xQuery)
        {
            xQuery.Limit(1);
            var (sql, bindings) = Compile(database, tableName, xQuery);

            using var connection = database.GetConnection();
            return await connection.QueryFirstOrDefaultAsync<TValue>(sql, bindings);
        }

        public static async Task<IEnumerable<TValue>> GetValueListAsync<TValue>(IDistributedCache cache, IDatabase database, string tableName, Query query = null)
        {
            var xQuery = NewQuery(tableName, query);
            var caching = await GetCachingAsync(xQuery, cache);
            if (caching != null)
            {
                return await cache.GetOrCreateAsync(caching.Key,
                    async () => await _GetValueListAsync<TValue>(database, tableName, xQuery),
                    caching.Options
                );
            }

            return await _GetValueListAsync<TValue>(database, tableName, xQuery);
        }

        private static async Task<IEnumerable<TValue>> _GetValueListAsync<TValue>(IDatabase database, string tableName, Query xQuery)
        {
            var (sql, bindings) = Compile(database, tableName, xQuery);

            using var connection = database.GetConnection();
            return await connection.QueryAsync<TValue>(sql, bindings);
        }

        public static async Task<int> MaxAsync(IDistributedCache cache, IDatabase database, string tableName, string columnName, Query query = null)
        {
            var xQuery = NewQuery(tableName, query);
            var caching = await GetCachingAsync(xQuery, cache);
            if (caching != null)
            {
                return await cache.GetOrCreateIntAsync(caching.Key,
                    async () => await _MaxAsync(database, tableName, columnName, xQuery),
                    caching.Options
                );
            }

            return await _MaxAsync(database, tableName, columnName, xQuery);
        }

        private static async Task<int> _MaxAsync(IDatabase database, string tableName, string columnName, Query xQuery)
        {
            xQuery.AsMax(columnName);
            var (sql, bindings) = Compile(database, tableName, xQuery);

            using var connection = database.GetConnection();
            var max = await connection.QueryFirstOrDefaultAsync<int?>(sql, bindings);
            return max ?? 0;
        }

        public static async Task<T> GetObjectAsync<T>(IDistributedCache cache, IDatabase database, string tableName, Query query = null) where T : Entity
        {
            var xQuery = NewQuery(tableName, query);
            var caching = await GetCachingAsync(xQuery, cache);
            if (caching != null)
            {
                return await cache.GetOrCreateAsync(caching.Key,
                    async () => await _GetObjectAsync<T>(cache, database, tableName, xQuery),
                    caching.Options
                );
            }

            return await _GetObjectAsync<T>(cache, database, tableName, xQuery);
        }

        private static async Task<T> _GetObjectAsync<T>(IDistributedCache cache, IDatabase database, string tableName, Query xQuery) where T : Entity
        {
            xQuery.ClearComponent("select").SelectRaw("*").Limit(1);
            var (sql, bindings) = Compile(database, tableName, xQuery);

            T value;
            using (var connection = database.GetConnection())
            {
                value = await connection.QueryFirstOrDefaultAsync<T>(sql, bindings);
            }

            await SyncAndCheckGuidAsync(cache, database, tableName, value);
            return value;
        }

        public static async Task<IEnumerable<T>> GetObjectListAsync<T>(IDistributedCache cache, IDatabase database, string tableName, Query query = null) where T : Entity
        {
            var xQuery = NewQuery(tableName, query);
            var caching = await GetCachingAsync(xQuery, cache);
            if (caching != null)
            {
                return await cache.GetOrCreateAsync(caching.Key,
                    async () => await _GetObjectListAsync<T>(cache, database, tableName, xQuery),
                    caching.Options
                );
            }

            return await _GetObjectListAsync<T>(cache, database, tableName, xQuery);
        }

        private static async Task<IEnumerable<T>> _GetObjectListAsync<T>(IDistributedCache cache, IDatabase database, string tableName, Query xQuery) where T : Entity
        {
            xQuery.ClearComponent("select").SelectRaw("*");
            var (sql, bindings) = Compile(database, tableName, xQuery);

            IEnumerable<T> values;
            using (var connection = database.GetConnection())
            {
                values = await connection.QueryAsync<T>(sql, bindings);
            }

            foreach (var dataInfo in values)
            {
                await SyncAndCheckGuidAsync(cache, database, tableName, dataInfo);
            }
            return values;
        }
    }
}
