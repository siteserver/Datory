using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Datory.Caching;
using Microsoft.Extensions.Caching.Distributed;
using SqlKata;

[assembly: InternalsVisibleTo("Datory.Data.Tests")]

namespace Datory.Utils
{
    internal static partial class RepositoryUtils
    {
        private static Query NewQuery(string tableName, Query query = null)
        {
            return query != null ? query.Clone().From(tableName) : new Query(tableName);
        }

        private static async Task<Query> NewQueryAsync(IDistributedCache cache, string tableName, Query query = null)
        {
            var q = query != null ? query.Clone().From(tableName) : new Query(tableName);
            var caching = q.GetOneComponent<CachingCondition>("cache");
            if (cache == null || caching == null || string.IsNullOrEmpty(caching.Key) || caching.IsCaching) return q;

            q.ClearComponent("cache");
            await cache.RemoveAsync(caching.Key);

            return q;
        }

        private static async Task<CachingCondition> GetCachingAsync(Query query, IDistributedCache cache)
        {
            var caching = query.GetOneComponent<CachingCondition>("cache");

            if (cache == null || caching == null || string.IsNullOrEmpty(caching.Key)) return null;
            if (caching.IsCaching) return caching;

            query.ClearComponent("cache");
            await cache.RemoveAsync(caching.Key);
            return null;
        }

        private static (string sql, Dictionary<string, object> namedBindings) Compile(IDatabase database, string tableName, Query query)
        {
            var method = query.Method;
            if (method == "update")
            {
                query.Method = "select";
            }

            string sql;
            Dictionary<string, object> namedBindings;

            var compiler = DbUtils.GetCompiler(database.DatabaseType, database.ConnectionString);
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
    }
}
