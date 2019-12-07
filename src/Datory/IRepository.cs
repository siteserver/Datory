using System.Collections.Generic;
using Microsoft.Extensions.Caching.Distributed;

namespace Datory
{
    public interface IRepository
    {
        IDatabase Database { get; }

        string TableName { get; }

        List<TableColumn> TableColumns { get; }
    }
}
