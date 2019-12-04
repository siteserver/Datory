using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Caching.Distributed;
using SqlKata;

[assembly: InternalsVisibleTo("Datory.Data.Tests")]

namespace Datory.Utils
{
    internal static partial class RepositoryUtils
    {
        public static async Task SyncAndCheckGuidAsync(IDistributedCache cache, IDatabase database, string tableName, Entity dataInfo)
        {
            if (dataInfo == null || dataInfo.Id <= 0) return;

            if (!string.IsNullOrEmpty(dataInfo.GetExtendColumnName()))
            {
                dataInfo.Sync(dataInfo.Get<string>(dataInfo.GetExtendColumnName()));
            }

            if (Utilities.IsGuid(dataInfo.Guid)) return;

            dataInfo.Guid = Utilities.GetGuid();
            dataInfo.LastModifiedDate = DateTime.Now;

            await UpdateAllAsync(cache, database, tableName, new Query()
                .Set(nameof(Entity.Guid), dataInfo.Guid)
                .Where(nameof(Entity.Id), dataInfo.Id)
            );
        }

        public static async Task<int> UpdateAllAsync(IDistributedCache cache, IDatabase database, string tableName, Query query)
        {
            var xQuery = await NewQueryAsync(cache, tableName, query);

            xQuery.Method = "update";

            var (sql, bindings) = Compile(database, tableName, xQuery);

            using var connection = database.GetConnection();
            return await connection.ExecuteAsync(sql, bindings);
        }

        public static async Task<int> IncrementAllAsync(IDistributedCache cache, IDatabase database, string tableName, string columnName, Query query, int num = 1)
        {
            var xQuery = await NewQueryAsync(cache, tableName, query);

            xQuery
                .ClearComponent("update")
                .SetRaw($"{columnName} = {DbUtils.ColumnIncrement(database.DatabaseType, columnName, num)}");

            return await UpdateAllAsync(cache, database, tableName, xQuery);
        }

        public static async Task<int> DecrementAllAsync(IDistributedCache cache, IDatabase database, string tableName, string columnName, Query query, int num = 1)
        {
            var xQuery = await NewQueryAsync(cache, tableName, query);

            xQuery
                .ClearComponent("update")
                .SetRaw($"{columnName} = {DbUtils.ColumnDecrement(database.DatabaseType, columnName, num)}");

            return await UpdateAllAsync(cache, database, tableName, xQuery);
        }
    }
}
