using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Dapper;
using SqlKata;

[assembly: InternalsVisibleTo("Datory.Tests")]

namespace Datory.Utils
{
    internal static class RepositoryUtils
    {
        private static Query NewQuery(string tableName, Query query = null)
        {
            return query != null ? query.Clone().From(tableName) : new Query(tableName);
        }

        private static (string sql, Dictionary<string, object> namedBindings) Compile(Database database, string tableName, Query query)
        {
            var method = query.Method;
            if (method == "update")
            {
                query.Method = "select";
            }

            string sql;
            Dictionary<string, object> namedBindings;

            var compiler = SqlUtils.GetCompiler(database);
            var compiled = compiler.Compile(query);

            if (method == "update")
            {
                var bindings = new List<object>();

                var setList = new List<string>();
                var components = query.GetComponents("update");
                components.Add(new BasicCondition
                {
                    Column = nameof(Entity.LastModifiedDate),
                    Value = DateTime.Now
                });
                foreach (var clause in components)
                {
                    if (clause is RawCondition raw)
                    {
                        var set = compiler.WrapIdentifiers(raw.Expression);
                        if (setList.Contains(set, StringComparer.OrdinalIgnoreCase))
                        {
                            continue;
                        }
                        setList.Add(set);
                        if (raw.Bindings != null)
                        {
                            bindings.AddRange(raw.Bindings);
                        }
                    }
                    else if (clause is BasicCondition basic)
                    {
                        var set = compiler.Wrap(basic.Column) + " = ?";
                        if (setList.Contains(set, StringComparer.OrdinalIgnoreCase))
                        {
                            continue;
                        }
                        setList.Add(set);
                        bindings.Add(basic.Value);
                    }
                }
                bindings.AddRange(compiled.Bindings);
                //var index = compiled.Sql.IndexOf(" WHERE ", StringComparison.Ordinal);
                //var where = string.Empty;
                //if (index != -1)
                //{
                //    where = compiled.Sql.Substring(index);
                //}

                var result = new SqlResult
                {
                    Query = query
                };
                var where = compiler.CompileWheres(result);
                sql = $"UPDATE {tableName} SET { string.Join(", ", setList)} {where}";

                //sql = Helper.ExpandParameters(sql, "?", bindings.ToArray());
                sql = Helper.ReplaceAll(sql, "?", i => "@p" + i);

                namedBindings = Helper.Flatten(bindings).Select((v, i) => new { i, v })
                    .ToDictionary(x => "@p" + x.i, x => x.v);
            }
            else
            {
                sql = compiled.Sql;
                namedBindings = compiled.NamedBindings;
            }

            return (sql, namedBindings);
        }

        public static void SyncAndCheckGuid(Database database, string tableName, Entity dataInfo)
        {
            if (dataInfo == null || dataInfo.Id <= 0) return;

            if (!string.IsNullOrEmpty(dataInfo.GetExtendColumnName()))
            {
                dataInfo.Sync(dataInfo.Get<string>(dataInfo.GetExtendColumnName()));
            }

            if (Utilities.IsGuid(dataInfo.Guid)) return;

            dataInfo.Guid = Utilities.GetGuid();
            dataInfo.LastModifiedDate = DateTime.Now;

            UpdateAll(database, tableName, new Query()
                .Set(nameof(Entity.Guid), dataInfo.Guid)
                .Where(nameof(Entity.Id), dataInfo.Id)
            );
        }

        public static bool Exists(Database database, string tableName, Query query = null)
        {
            var xQuery = NewQuery(tableName, query);
            xQuery.ClearComponent("select").SelectRaw("COUNT(1)").ClearComponent("order");
            var (sql, bindings) = Compile(database, tableName, xQuery);

            return database.Connection.ExecuteScalar<bool>(sql, bindings);
        }

        public static int Count(Database database, string tableName, Query query = null)
        {
            var xQuery = NewQuery(tableName, query);
            xQuery.ClearComponent("order").AsCount();
            var (sql, bindings) = Compile(database, tableName, xQuery);

            return database.Connection.ExecuteScalar<int>(sql, bindings);
        }

        private static string GetFirstSelectColumnName(Query query)
        {
            string column = null;

            var components = query?.GetComponents("select");
            if (components == null) return null;

            foreach (var clause in components)
            {
                if (!(clause is Column select)) continue;
                column = select.Name;
                break;
            }

            return column;
        }

        public static int Sum(Database database, string tableName, Query query = null)
        {
            var xQuery = NewQuery(tableName, query);

            var columnName = GetFirstSelectColumnName(xQuery);
            if (string.IsNullOrEmpty(columnName)) throw new ArgumentNullException(nameof(query.Select));

            xQuery.AsSum(columnName);
            var (sql, bindings) = Compile(database, tableName, xQuery);

            return database.Connection.ExecuteScalar<int>(sql, bindings);
        }

        public static TValue GetValue<TValue>(Database database, string tableName, Query query)
        {
            if (query == null) return default(TValue);

            var xQuery = NewQuery(tableName, query);
            xQuery.Limit(1);
            var (sql, bindings) = Compile(database, tableName, xQuery);

            return database.Connection.QueryFirstOrDefault<TValue>(sql, bindings);
        }

        public static IList<TValue> GetValueList<TValue>(Database database, string tableName, Query query = null)
        {
            var xQuery = NewQuery(tableName, query);
            var (sql, bindings) = Compile(database, tableName, xQuery);

            return database.Connection.Query<TValue>(sql, bindings).ToList();
        }

        public static int? Max(Database database, string tableName, Query query = null)
        {
            var xQuery = NewQuery(tableName, query);

            var columnName = GetFirstSelectColumnName(xQuery);
            if (string.IsNullOrEmpty(columnName)) throw new ArgumentNullException($"{nameof(Query)}.{nameof(Query.Select)}");

            xQuery.AsMax(columnName);
            var (sql, bindings) = Compile(database, tableName, xQuery);

            return database.Connection.QueryFirstOrDefault<int?>(sql, bindings);
        }

        public static T GetObject<T>(Database database, string tableName, Query query = null) where T : Entity
        {
            var xQuery = NewQuery(tableName, query);
            xQuery.ClearComponent("select").SelectRaw("*").Limit(1);
            var (sql, bindings) = Compile(database, tableName, xQuery);

            var value = database.Connection.QueryFirstOrDefault<T>(sql, bindings);
            SyncAndCheckGuid(database, tableName, value);
            return value;
        }

        public static IList<T> GetObjectList<T>(Database database, string tableName, Query query = null) where T : Entity
        {
            var xQuery = NewQuery(tableName, query);
            xQuery.ClearComponent("select").SelectRaw("*");
            var (sql, bindings) = Compile(database, tableName, xQuery);

            var values = database.Connection.Query<T>(sql, bindings).ToList();
            foreach (var dataInfo in values)
            {
                SyncAndCheckGuid(database, tableName, dataInfo);
            }
            return values;
        }

        public static int InsertObject<T>(Database database, string tableName, IEnumerable<TableColumn> tableColumns, T dataInfo) where T : Entity
        {
            if (dataInfo == null) return 0;
            dataInfo.Guid = Utilities.GetGuid();
            dataInfo.LastModifiedDate = DateTime.Now;

            var dictionary = new Dictionary<string, object>();
            foreach (var tableColumn in tableColumns)
            {
                if (Utilities.EqualsIgnoreCase(tableColumn.AttributeName, nameof(Entity.Id))) continue;

                var value = tableColumn.IsExtend
                    ? Utilities.JsonSerialize(dataInfo.ToDictionary(dataInfo.GetColumnNames()))
                    : dataInfo.Get(tableColumn.AttributeName);

                dictionary[tableColumn.AttributeName] = value;
            }

            var xQuery = NewQuery(tableName);
            xQuery.AsInsert(dictionary, true);
            var (sql, bindings) = Compile(database, tableName, xQuery);

            dataInfo.Id = database.Connection.QueryFirst<int>(sql, bindings);
            return dataInfo.Id;
        }

        public static int DeleteAll(Database database, string tableName, Query query = null)
        {
            var xQuery = NewQuery(tableName, query);
            xQuery.AsDelete();

            var (sql, bindings) = Compile(database, tableName, xQuery);

            return database.Connection.Execute(sql, bindings);
        }

        public static int UpdateAll(Database database, string tableName, Query query)
        {
            var xQuery = NewQuery(tableName, query);

            xQuery.Method = "update";

            var (sql, bindings) = Compile(database, tableName, xQuery);

            return database.Connection.Execute(sql, bindings);
        }

        public static int IncrementAll(Database database, string tableName, Query query, int num = 1)
        {
            var xQuery = NewQuery(tableName, query);

            var columnName = GetFirstSelectColumnName(xQuery);
            if (string.IsNullOrEmpty(columnName)) throw new ArgumentNullException(nameof(query.Select));

            xQuery
                .ClearComponent("update")
                .SetRaw($"{columnName} = {SqlUtils.ColumnIncrement(database.DatabaseType, columnName, num)}");

            return UpdateAll(database, tableName, xQuery);
        }

        public static int DecrementAll(Database database, string tableName, Query query, int num = 1)
        {
            var xQuery = NewQuery(tableName, query);

            var columnName = GetFirstSelectColumnName(xQuery);
            if (string.IsNullOrEmpty(columnName)) throw new ArgumentNullException(nameof(query.Select));

            xQuery
                .ClearComponent("update")
                .SetRaw($"{columnName} = {SqlUtils.ColumnDecrement(database.DatabaseType, columnName, num)}");

            return UpdateAll(database, tableName, xQuery);
        }
    }
}
