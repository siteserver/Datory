using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text;
using System.Threading.Tasks;
using SqlKata.Compilers;

namespace Datory.DatabaseImpl
{
    internal class ShenTongImpl : IDatabaseImpl
    {
        public DbConnection GetConnection(string connectionString)
        {
            throw new NotImplementedException();
        }

        public Compiler GetCompiler(string connectionString)
        {
            throw new NotImplementedException();
        }

        public bool IsUseLegacyPagination(string connectionString)
        {
            throw new NotImplementedException();
        }

        public Task<List<TableColumn>> GetTableColumnsAsync(string connectionString, string tableName)
        {
            throw new NotImplementedException();
        }

        public Task<List<string>> GetDatabaseNamesAsync(string connectionString)
        {
            throw new NotImplementedException();
        }

        public Task<List<string>> GetTableNamesAsync(string connectionString)
        {
            throw new NotImplementedException();
        }

        public string ColumnIncrement(string columnName, int plusNum = 1)
        {
            throw new NotImplementedException();
        }

        public string ColumnDecrement(string columnName, int minusNum = 1)
        {
            throw new NotImplementedException();
        }

        public string GetAutoIncrementDataType(bool alterTable = false)
        {
            throw new NotImplementedException();
        }

        public string GetColumnSqlString(TableColumn tableColumn)
        {
            throw new NotImplementedException();
        }

        public string GetPrimaryKeySqlString(string tableName, string attributeName)
        {
            throw new NotImplementedException();
        }

        public string GetQuotedIdentifier(string identifier)
        {
            throw new NotImplementedException();
        }

        public string GetAddColumnsSqlString(string tableName, string columnsSqlString)
        {
            throw new NotImplementedException();
        }
    }
}
