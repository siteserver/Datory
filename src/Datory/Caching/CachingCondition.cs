using Microsoft.Extensions.Caching.Distributed;
using SqlKata;

namespace Datory.Caching
{
    public class CachingCondition : AbstractClause
    {
        public bool IsCaching { get; set; }
        public string Key { get; set; }
        public DistributedCacheEntryOptions Options { get; set; }

        /// <inheritdoc />
        public override AbstractClause Clone()
        {
            return new CachingCondition
            {
                Engine = Engine,
                IsCaching = IsCaching,
                Key = Key,
                Options = Options,
                Component = Component,
            };
        }
    }
}
