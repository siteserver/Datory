using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Dapper;
using Datory.Utils;

[assembly: InternalsVisibleTo("Datory.Tests")]

namespace Datory
{
    public static class DatoryUtils
    {
        public const int VarCharDefaultLength = 500;

        private static readonly ConcurrentDictionary<Database, bool> UseLegacyPagination = new ConcurrentDictionary<Database, bool>();

        public static bool IsUseLegacyPagination(Database database)
        {
            if (UseLegacyPagination.TryGetValue(database, out var useLegacyPagination)) return useLegacyPagination;
            useLegacyPagination = false;

            if (database.DatabaseType == DatabaseType.SqlServer)
            {
                const string sqlString = "select left(cast(serverproperty('productversion') as varchar), 4)";

                try
                {
                    using (database.Connection)
                    {
                        var version = database.Connection.ExecuteScalar<string>(sqlString);

                        useLegacyPagination = ConvertUtils.ToDecimal(version) < 11;
                    }
                }
                catch
                {
                    // ignored
                }
            }

            UseLegacyPagination[database] = useLegacyPagination;

            return useLegacyPagination;
        }

        public static bool IsTableExists(Database database, string tableName)
        {
            bool exists;

            if (database.DatabaseType == DatabaseType.Oracle)
            {
                tableName = tableName.ToUpper();
            }
            else if (database.DatabaseType == DatabaseType.MySql || database.DatabaseType == DatabaseType.PostgreSql)
            {
                tableName = tableName.ToLower();
            }

            try
            {
                // ANSI SQL way.  Works in PostgreSQL, MSSQL, MySQL.  
                if (database.DatabaseType != DatabaseType.Oracle)
                {
                    using (database.Connection)
                    {
                        var sql = $"select case when exists((select * from information_schema.tables where table_name = '{tableName}')) then 1 else 0 end";

                        exists = database.Connection.ExecuteScalar<int>(sql) == 1;
                    }
                }
                else
                {
                    using (database.Connection)
                    {
                        var sql = $"SELECT COUNT(*) FROM ALL_OBJECTS WHERE OBJECT_TYPE = 'TABLE' AND OWNER = '{SqlUtils.GetConnectionStringUserId(database.ConnectionString).ToUpper()}' and OBJECT_NAME = '{tableName}'";

                        exists = database.Connection.ExecuteScalar<int>(sql) == 1;
                    }
                }
            }
            catch
            {
                try
                {
                    // Other DB.  Graceful degradation
                    using (database.Connection)
                    {
                        var sql = $"select 1 from {tableName} where 1 = 0";

                        exists = database.Connection.ExecuteScalar<int>(sql) == 1;
                    }
                }
                catch
                {
                    exists = false;
                }
            }

            return exists;
        }

        public static string AddIdentityColumnIdIfNotExists(Database database, string tableName, List<TableColumn> columns)
        {
            var identityColumnName = string.Empty;
            foreach (var column in columns)
            {
                if (column.IsIdentity || ConvertUtils.EqualsIgnoreCase(column.AttributeName, "id"))
                {
                    identityColumnName = column.AttributeName;
                    break;
                }
            }

            if (string.IsNullOrEmpty(identityColumnName))
            {
                identityColumnName = nameof(Entity.Id);
                var sqlString =
                    SqlUtils.GetAddColumnsSqlString(database.DatabaseType, tableName, $"{identityColumnName} {SqlUtils.GetAutoIncrementDataType(database.DatabaseType, true)}");

                using (database.Connection)
                {
                    database.Connection.Execute(sqlString);
                }

                columns.Insert(0, new TableColumn
                {
                    AttributeName = identityColumnName,
                    DataType = DataType.Integer,
                    IsPrimaryKey = false,
                    IsIdentity = true
                });
            }

            return identityColumnName;
        }

        public static void AlterTable(Database database, string tableName, IList<TableColumn> tableColumns, IList<string> dropColumnNames)
        {
            var list = new List<string>();

            var columnNameList = GetColumnNames(database, tableName);
            foreach (var tableColumn in tableColumns)
            {
                if (!ConvertUtils.ContainsIgnoreCase(columnNameList, tableColumn.AttributeName))
                {
                    list.Add(SqlUtils.GetAddColumnsSqlString(database.DatabaseType, tableName, SqlUtils.GetColumnSqlString(database.DatabaseType, tableColumn)));
                }
            }

            if (dropColumnNames != null)
            {
                foreach (var columnName in columnNameList)
                {
                    if (ConvertUtils.ContainsIgnoreCase(dropColumnNames, columnName))
                    {
                        list.Add(SqlUtils.GetDropColumnsSqlString(database.DatabaseType, tableName, columnName));
                    }
                }
            }

            if (list.Count <= 0) return;

            using (database.Connection)
            {
                foreach (var sqlString in list)
                {
                    database.Connection.Execute(sqlString);
                }
            }
        }

        public static void CreateTable(Database database, string tableName, List<TableColumn> tableColumns)
        {
            var sqlBuilder = new StringBuilder();

            sqlBuilder.Append($@"CREATE TABLE {tableName} (").AppendLine();

            var primaryKeyColumns = new List<TableColumn>();
            TableColumn identityColumn = null;

            foreach (var tableColumn in tableColumns)
            {
                if (ConvertUtils.EqualsIgnoreCase(tableColumn.AttributeName, nameof(Entity.Id)))
                {
                    tableColumn.DataType = DataType.Integer;
                    tableColumn.IsIdentity = true;
                    tableColumn.IsPrimaryKey = true;
                }
                else if (ConvertUtils.EqualsIgnoreCase(tableColumn.AttributeName, nameof(Entity.Guid)))
                {
                    tableColumn.DataType = DataType.VarChar;
                    tableColumn.DataLength = 50;
                }
                else if (ConvertUtils.EqualsIgnoreCase(tableColumn.AttributeName, nameof(Entity.LastModifiedDate)))
                {
                    tableColumn.DataType = DataType.DateTime;
                }
            }

            foreach (var tableColumn in tableColumns)
            {
                if (string.IsNullOrEmpty(tableColumn.AttributeName)) continue;

                if (tableColumn.IsIdentity)
                {
                    identityColumn = tableColumn;
                }

                if (tableColumn.IsPrimaryKey)
                {
                    primaryKeyColumns.Add(tableColumn);
                }

                if (tableColumn.DataType == DataType.VarChar && tableColumn.DataLength == 0)
                {
                    tableColumn.DataLength = VarCharDefaultLength;
                }

                var columnSql = SqlUtils.GetColumnSqlString(database.DatabaseType, tableColumn);
                if (!string.IsNullOrEmpty(columnSql))
                {
                    sqlBuilder.Append(columnSql).Append(",");
                }
            }

            if (identityColumn != null)
            {
                var primaryKeySql = SqlUtils.GetPrimaryKeySqlString(database.DatabaseType, tableName, identityColumn.AttributeName);
                if (!string.IsNullOrEmpty(primaryKeySql))
                {
                    sqlBuilder.Append(primaryKeySql).Append(",");
                }
            }
            else if (primaryKeyColumns.Count > 0)
            {
                foreach (var tableColumn in primaryKeyColumns)
                {
                    var primaryKeySql = SqlUtils.GetPrimaryKeySqlString(database.DatabaseType, tableName, tableColumn.AttributeName);
                    if (!string.IsNullOrEmpty(primaryKeySql))
                    {
                        sqlBuilder.Append(primaryKeySql).Append(",");
                    }
                }
            }

            sqlBuilder.Length--;

            sqlBuilder.AppendLine().Append(database.DatabaseType == DatabaseType.MySql
                ? ") ENGINE=InnoDB DEFAULT CHARSET=utf8mb4"
                : ")");
            
            using (database.Connection)
            {
                database.Connection.Execute(sqlBuilder.ToString());
            }
        }

        public static void CreateIndex(Database database, string tableName, string indexName, params string[] columns)
        {
            var sqlString = new StringBuilder($@"CREATE INDEX {SqlUtils.GetQuotedIdentifier(database.DatabaseType, indexName)} ON {SqlUtils.GetQuotedIdentifier(database.DatabaseType, tableName)}(");

            foreach (var column in columns)
            {
                var columnName = column;
                var columnOrder = "ASC";
                var i = column.IndexOf(" ", StringComparison.Ordinal);
                if (i != -1)
                {
                    columnName = column.Substring(0, i);
                    columnOrder = column.Substring(i + 1);
                }
                sqlString.Append($"{SqlUtils.GetQuotedIdentifier(database.DatabaseType, columnName)} {columnOrder}, ");
            }

            sqlString.Length--;
            sqlString.Append(")");

            using (database.Connection)
            {
                database.Connection.Execute(sqlString.ToString());
            }
        }

        public static List<string> GetColumnNames(Database database, string tableName)
        {
            var allTableColumnInfoList = GetTableColumns(database, tableName);
            return allTableColumnInfoList.Select(tableColumnInfo => tableColumnInfo.AttributeName).ToList();
        }

        public static List<TableColumn> GetTableColumns<T>() where T : Entity
        {
            return ReflectionUtils.GetTableColumns(typeof(T));
        }

        public static List<TableColumn> GetTableColumns(Database database, string tableName)
        {
            List<TableColumn> list = null;

            if (database.DatabaseType == DatabaseType.MySql)
            {
                list = SqlUtils.GetMySqlColumns(database, tableName);
            }
            else if (database.DatabaseType == DatabaseType.SqlServer)
            {
                list = SqlUtils.GetSqlServerColumns(database, tableName);
            }
            else if (database.DatabaseType == DatabaseType.PostgreSql)
            {
                list = SqlUtils.GetPostgreSqlColumns(database, tableName);
            }
            else if (database.DatabaseType == DatabaseType.Oracle)
            {
                list = SqlUtils.GetOracleColumns(database, tableName);
            }

            return list;
        }

        public static List<string> GetTableNames(Database database)
        {
            var sqlString = string.Empty;

            if (database.DatabaseType == DatabaseType.MySql)
            {
                sqlString = $"SELECT table_name FROM information_schema.tables WHERE table_schema='{database.Owner}' ORDER BY table_name";
            }
            else if (database.DatabaseType == DatabaseType.SqlServer)
            {
                sqlString =
                    $"SELECT name FROM [{database.Owner}]..sysobjects WHERE type = 'U' AND category<>2 ORDER BY Name";
            }
            else if (database.DatabaseType == DatabaseType.PostgreSql)
            {
                sqlString =
                    $"SELECT table_name FROM information_schema.tables WHERE table_catalog = '{database.Owner}' AND table_type = 'BASE TABLE' AND table_schema NOT IN ('pg_catalog', 'information_schema')";
            }
            else if (database.DatabaseType == DatabaseType.Oracle)
            {
                sqlString = "select TABLE_NAME from user_tables";
            }

            if (string.IsNullOrEmpty(sqlString)) return new List<string>();

            IEnumerable<string> tableNames;
            using (database.Connection)
            {
                tableNames = database.Connection.Query<string>(sqlString);
            }
            return tableNames.Where(tableName => !string.IsNullOrEmpty(tableName)).ToList();
        }

        public static void DropTable(Database database, string tableName)
        {
            using (database.Connection)
            {
                var sql = $"DROP TABLE {tableName}";

                database.Connection.Execute(sql);
            }
        }

        //public static bool DropTable(Database database, string tableName, out Exception ex)
        //{
        //    ex = null;
        //    var isAltered = false;

        //    try
        //    {
        //        using (database.Connection)
        //        {
        //            database.Connection.Execute($"DROP TABLE {tableName}");
        //        }

        //        isAltered = true;
        //    }
        //    catch (Exception e)
        //    {
        //        ex = e;
        //    }

        //    return isAltered;
        //}
    }
}
