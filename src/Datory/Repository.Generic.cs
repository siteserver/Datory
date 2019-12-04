using System.Collections.Generic;
using Datory.Utils;
using Microsoft.Extensions.Caching.Distributed;

namespace Datory
{
    public partial class Repository<T> : IRepository where T : Entity, new()
    {
        public IDatabase Database { get; }
        public string TableName { get; }
        public List<TableColumn> TableColumns { get; }
        public IDistributedCache Cache { get; }

        public Repository(IDatabase database)
        {
            Database = database;
            TableName = ReflectionUtils.GetTableName(typeof(T));
            TableColumns = ReflectionUtils.GetTableColumns(typeof(T));
        }

        public Repository(IDatabase database, IDistributedCache cache)
        {
            Database = database;
            TableName = ReflectionUtils.GetTableName(typeof(T));
            TableColumns = ReflectionUtils.GetTableColumns(typeof(T));
            Cache = cache;
        }

        public Repository(IDatabase database, string tableName)
        {
            Database = database;
            TableName = tableName;
            TableColumns = ReflectionUtils.GetTableColumns(typeof(T));
        }

        public Repository(IDatabase database, string tableName, IDistributedCache cache)
        {
            Database = database;
            TableName = tableName;
            TableColumns = ReflectionUtils.GetTableColumns(typeof(T));
            Cache = cache;
        }
    }
}
