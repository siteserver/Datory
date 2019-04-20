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

        

        private static readonly ConcurrentDictionary<string, bool> UseLegacyPagination = new ConcurrentDictionary<string, bool>();

        public static bool IsUseLegacyPagination(DatabaseType databaseType, string connectionString)
        {
            var database = $"{databaseType.Value}:{connectionString}";

            if (UseLegacyPagination.TryGetValue(database, out var useLegacyPagination)) return useLegacyPagination;
            useLegacyPagination = false;

            if (databaseType == DatabaseType.SqlServer)
            {
                const string sqlString = "select left(cast(serverproperty('productversion') as varchar), 4)";

                try
                {
                    using (var connection = new Connection(databaseType, connectionString))
                    {
                        var version = connection.ExecuteScalar<string>(sqlString);

                        useLegacyPagination = Utilities.ToDecimal(version) < 11;
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

        public static bool IsTableExists(DatabaseType databaseType, string connectionString, string tableName)
        {
            bool exists;

            if (databaseType == DatabaseType.Oracle)
            {
                tableName = tableName.ToUpper();
            }
            else if (databaseType == DatabaseType.MySql || databaseType == DatabaseType.PostgreSql)
            {
                tableName = tableName.ToLower();
            }

            try
            {
                // ANSI SQL way.  Works in PostgreSQL, MSSQL, MySQL.  
                if (databaseType != DatabaseType.Oracle)
                {
                    var sql = $"select case when exists((select * from information_schema.tables where table_name = '{tableName}')) then 1 else 0 end";

                    using (var connection = new Connection(databaseType, connectionString))
                    {
                        exists = connection.ExecuteScalar<int>(sql) == 1;
                    }
                }
                else
                {
                    var userName = Utilities.GetConnectionStringUserName(connectionString);
                    var sql = $"SELECT COUNT(*) FROM ALL_OBJECTS WHERE OBJECT_TYPE = 'TABLE' AND OWNER = '{userName.ToUpper()}' and OBJECT_NAME = '{tableName}'";

                    using (var connection = new Connection(databaseType, connectionString))
                    {
                        exists = connection.ExecuteScalar<int>(sql) == 1;
                    }
                }
            }
            catch
            {
                try
                {
                    var sql = $"select 1 from {tableName} where 1 = 0";

                    using (var connection = new Connection(databaseType, connectionString))
                    {
                        exists = connection.ExecuteScalar<int>(sql) == 1;
                    }
                }
                catch
                {
                    exists = false;
                }
            }

            return exists;
        }

        public static string AddIdentityColumnIdIfNotExists(DatabaseType databaseType, string connectionString, string tableName, List<TableColumn> columns)
        {
            var identityColumnName = string.Empty;
            foreach (var column in columns)
            {
                if (column.IsIdentity || Utilities.EqualsIgnoreCase(column.AttributeName, "id"))
                {
                    identityColumnName = column.AttributeName;
                    break;
                }
            }

            if (string.IsNullOrEmpty(identityColumnName))
            {
                identityColumnName = nameof(Entity.Id);
                var sqlString =
                    SqlUtils.GetAddColumnsSqlString(databaseType, tableName, $"{identityColumnName} {SqlUtils.GetAutoIncrementDataType(databaseType, true)}");

                using (var connection = new Connection(databaseType, connectionString))
                {
                    connection.Execute(sqlString);
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

        public static void AlterTable(DatabaseType databaseType, string connectionString, string tableName, IList<TableColumn> tableColumns, IList<string> dropColumnNames = null)
        {
            var list = new List<string>();

            var columnNameList = GetColumnNames(databaseType, connectionString, tableName);
            foreach (var tableColumn in tableColumns)
            {
                if (!Utilities.ContainsIgnoreCase(columnNameList, tableColumn.AttributeName))
                {
                    list.Add(SqlUtils.GetAddColumnsSqlString(databaseType, tableName, SqlUtils.GetColumnSqlString(databaseType, tableColumn)));
                }
            }

            if (dropColumnNames != null)
            {
                foreach (var columnName in columnNameList)
                {
                    if (Utilities.ContainsIgnoreCase(dropColumnNames, columnName))
                    {
                        list.Add(SqlUtils.GetDropColumnsSqlString(databaseType, tableName, columnName));
                    }
                }
            }

            if (list.Count <= 0) return;

            foreach (var sqlString in list)
            {
                using (var connection = new Connection(databaseType, connectionString))
                {
                    connection.Execute(sqlString);
                }
            }
        }

        public static void CreateTable(DatabaseType databaseType, string connectionString, string tableName, List<TableColumn> tableColumns)
        {
            var sqlBuilder = new StringBuilder();

            sqlBuilder.Append($@"CREATE TABLE {SqlUtils.GetQuotedIdentifier(databaseType, tableName)} (").AppendLine();

            var primaryKeyColumns = new List<TableColumn>();
            TableColumn identityColumn = null;

            foreach (var tableColumn in tableColumns)
            {
                if (Utilities.EqualsIgnoreCase(tableColumn.AttributeName, nameof(Entity.Id)))
                {
                    tableColumn.DataType = DataType.Integer;
                    tableColumn.IsIdentity = true;
                    tableColumn.IsPrimaryKey = true;
                }
                else if (Utilities.EqualsIgnoreCase(tableColumn.AttributeName, nameof(Entity.Guid)))
                {
                    tableColumn.DataType = DataType.VarChar;
                    tableColumn.DataLength = 50;
                }
                else if (Utilities.EqualsIgnoreCase(tableColumn.AttributeName, nameof(Entity.LastModifiedDate)))
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

                var columnSql = SqlUtils.GetColumnSqlString(databaseType, tableColumn);
                if (!string.IsNullOrEmpty(columnSql))
                {
                    sqlBuilder.Append(columnSql).Append(",");
                }
            }

            if (identityColumn != null)
            {
                var primaryKeySql = SqlUtils.GetPrimaryKeySqlString(databaseType, tableName, identityColumn.AttributeName);
                if (!string.IsNullOrEmpty(primaryKeySql))
                {
                    sqlBuilder.Append(primaryKeySql).Append(",");
                }
            }
            else if (primaryKeyColumns.Count > 0)
            {
                foreach (var tableColumn in primaryKeyColumns)
                {
                    var primaryKeySql = SqlUtils.GetPrimaryKeySqlString(databaseType, tableName, tableColumn.AttributeName);
                    if (!string.IsNullOrEmpty(primaryKeySql))
                    {
                        sqlBuilder.Append(primaryKeySql).Append(",");
                    }
                }
            }

            sqlBuilder.Length--;

            sqlBuilder.AppendLine().Append(databaseType == DatabaseType.MySql
                ? ") ENGINE=InnoDB DEFAULT CHARSET=utf8mb4"
                : ")");

            using (var connection = new Connection(databaseType, connectionString))
            {
                connection.Execute(sqlBuilder.ToString());
            }
        }

        public static void CreateIndex(DatabaseType databaseType, string connectionString, string tableName, string indexName, params string[] columns)
        {
            var sqlString = new StringBuilder($@"CREATE INDEX {SqlUtils.GetQuotedIdentifier(databaseType, indexName)} ON {SqlUtils.GetQuotedIdentifier(databaseType, tableName)}(");

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
                sqlString.Append($"{SqlUtils.GetQuotedIdentifier(databaseType, columnName)} {columnOrder}, ");
            }

            sqlString.Length--;
            sqlString.Append(")");

            using (var connection = new Connection(databaseType, connectionString))
            {
                connection.Execute(sqlString.ToString());
            }
        }

        public static List<string> GetColumnNames(DatabaseType databaseType, string connectionString, string tableName)
        {
            var allTableColumnInfoList = GetTableColumns(databaseType, connectionString, tableName);
            return allTableColumnInfoList.Select(tableColumnInfo => tableColumnInfo.AttributeName).ToList();
        }

        public static List<TableColumn> GetTableColumns<T>() where T : Entity
        {
            return ReflectionUtils.GetTableColumns(typeof(T));
        }

        public static List<TableColumn> GetTableColumns(DatabaseType databaseType, string connectionString, string tableName)
        {
            List<TableColumn> list = null;

            if (databaseType == DatabaseType.MySql)
            {
                list = SqlUtils.GetMySqlColumns(databaseType, connectionString, tableName);
            }
            else if (databaseType == DatabaseType.SqlServer)
            {
                list = SqlUtils.GetSqlServerColumns(databaseType, connectionString, tableName);
            }
            else if (databaseType == DatabaseType.PostgreSql)
            {
                list = SqlUtils.GetPostgreSqlColumns(databaseType, connectionString, tableName);
            }
            else if (databaseType == DatabaseType.Oracle)
            {
                list = SqlUtils.GetOracleColumns(databaseType, connectionString, tableName);
            }

            return list;
        }

        public static List<string> GetTableNames(DatabaseType databaseType, string connectionString)
        {
            IEnumerable<string> tableNames;

            using (var connection = new Connection(databaseType, connectionString))
            {
                var sqlString = string.Empty;

                if (databaseType == DatabaseType.MySql)
                {
                    sqlString = $"SELECT table_name FROM information_schema.tables WHERE table_schema='{connection.Database}' ORDER BY table_name";
                }
                else if (databaseType == DatabaseType.SqlServer)
                {
                    sqlString =
                        $"SELECT name FROM [{connection.Database}]..sysobjects WHERE type = 'U' AND category<>2 ORDER BY Name";
                }
                else if (databaseType == DatabaseType.PostgreSql)
                {
                    sqlString =
                        $"SELECT table_name FROM information_schema.tables WHERE table_catalog = '{connection.Database}' AND table_type = 'BASE TABLE' AND table_schema NOT IN ('pg_catalog', 'information_schema')";
                }
                else if (databaseType == DatabaseType.Oracle)
                {
                    sqlString = "select TABLE_NAME from user_tables";
                }

                if (string.IsNullOrEmpty(sqlString)) return new List<string>();

                tableNames = connection.Query<string>(sqlString);
            }

            return tableNames.Where(tableName => !string.IsNullOrEmpty(tableName)).ToList();
        }

        public static void DropTable(DatabaseType databaseType, string connectionString, string tableName)
        {
            using (var connection = new Connection(databaseType, connectionString))
            {
                connection.Execute($"DROP TABLE {SqlUtils.GetQuotedIdentifier(databaseType, tableName)}");
            }
        }
    }
}
