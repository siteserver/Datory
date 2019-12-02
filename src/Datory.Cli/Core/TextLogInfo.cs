using System;
using Datory.Utils;

namespace Datory.Cli.Core
{
    public class TextLogInfo
    {
        public DateTime DateTime { private get; set; }
        public string Detail { private get; set; }
        public Exception Exception { private get; set; }

        public override string ToString()
        {
            return Utilities.JsonSerialize(new
            {
                DateTime,
                Detail,
                Exception?.Message,
                Exception?.StackTrace
            });
        }
    }
}
