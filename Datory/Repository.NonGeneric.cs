using System.Collections.Generic;

namespace Datory
{
    public partial class Repository : IRepository
    {
        public Database Database { get; }
        public string TableName { get; }
        public List<TableColumn> TableColumns { get; }

        public Repository(Database database)
        {
            Database = database;
            TableName = null;
            TableColumns = null;
        }

        public Repository(Database database, string tableName)
        {
            Database = database;
            TableName = tableName;
            TableColumns = null;
        }

        public Repository(Database database, string tableName, List<TableColumn> tableColumns)
        {
            Database = database;
            TableName = tableName;
            TableColumns = tableColumns;
        }
    }
}
