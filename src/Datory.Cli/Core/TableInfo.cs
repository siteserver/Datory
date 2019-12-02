using System.Collections.Generic;

namespace Datory.Cli.Core
{
    public class TableInfo
    {
        public IList<TableColumn> Columns { get; set; }
        public int TotalCount { get; set; }
        public IList<string> RowFiles { get; set; }
    }
}
