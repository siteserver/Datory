using System.Collections.Generic;

namespace Datory
{
    public interface IRepository
    {
        Database Database { get; }

        string TableName { get; }

        List<TableColumn> TableColumns { get; }
    }
}
