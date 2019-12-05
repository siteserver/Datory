using System;
using System.Collections.Generic;
using System.Text;
using Datory.Caching;

namespace Datory.Utils
{
    public class CompileInfo
    {
        public string Sql { get; set; }
        public Dictionary<string, object> NamedBindings { get; set; }
        public CachingCondition Caching { get; set; }
    }
}
