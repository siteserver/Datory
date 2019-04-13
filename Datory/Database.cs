using System;
using System.Data;
using Datory.Utils;

namespace Datory
{
    public class Database
    {
        public DatabaseType DatabaseType { get; }

        public string ConnectionString { get; }

        public string Name { get; }

        public string Owner { get; }

        public IDbConnection Connection { get; }

        public Database(DatabaseType databaseType, IDbConnection connection)
        {
            DatabaseType = databaseType;
            Connection = connection;

            ConnectionString = connection.ConnectionString;
            Name = connection.Database;

            //if (databaseType == DatabaseType.Oracle)
            //{
            //    var index1 = connectionString.IndexOf("SERVICE_NAME=", StringComparison.Ordinal);
            //    var index2 = connectionString.IndexOf(")));", StringComparison.Ordinal);
            //    Name = connectionString.Substring(index1 + 13, index2 - index1 - 13);
            //}
            //else
            //{
            //    Name = SqlUtils.GetValueFromConnectionString(ConnectionString, "Database");
            //}

            Owner = GetOwner(ConnectionString);
        }

        private static string GetOwner(string connectionString)
        {
            var userId = string.Empty;

            foreach (var pair in Utilities.StringCollectionToStringList(connectionString, ';'))
            {
                if (!string.IsNullOrEmpty(pair) && pair.IndexOf("=", StringComparison.Ordinal) != -1)
                {
                    var key = pair.Substring(0, pair.IndexOf("=", StringComparison.Ordinal));
                    var value = pair.Substring(pair.IndexOf("=", StringComparison.Ordinal) + 1);
                    if (Utilities.EqualsIgnoreCase(key, "Uid") ||
                        Utilities.EqualsIgnoreCase(key, "Username") ||
                        Utilities.EqualsIgnoreCase(key, "User ID"))
                    {
                        return value;
                    }
                }
            }

            return userId;
        }
    }
}
