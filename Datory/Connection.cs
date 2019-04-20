using System.Data;
using Datory.Utils;

namespace Datory
{
    public class Connection : IDbConnection
    {
        private readonly IDbConnection _dbConnection;
        public DatabaseType DatabaseType { get; }

        public Connection(DatabaseType databaseType, string connectionString)
        {
            DatabaseType = databaseType;
            ConnectionString = connectionString;
            _dbConnection = SqlUtils.GetConnection(databaseType, connectionString);
        }

        public IDbTransaction BeginTransaction()
        {
            return _dbConnection.BeginTransaction();
        }

        public IDbTransaction BeginTransaction(IsolationLevel il)
        {
            return _dbConnection.BeginTransaction(il);
        }

        public void ChangeDatabase(string databaseName)
        {
            _dbConnection.ChangeDatabase(databaseName);
        }

        public void Close()
        {
            _dbConnection.Close();
        }

        public IDbCommand CreateCommand()
        {
            return _dbConnection.CreateCommand();
        }

        public void Open()
        {
            _dbConnection.Open();
        }

        public string ConnectionString { get; set; }
        public int ConnectionTimeout => _dbConnection.ConnectionTimeout;

        public string Database => _dbConnection.Database;

        public ConnectionState State => _dbConnection.State;

        public void Dispose()
        {
            _dbConnection.Dispose();
        }
    }
}
