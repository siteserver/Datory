using Microsoft.Extensions.Caching.Distributed;
using SqlKata;

namespace Datory.Caching
{
    public class CachingCondition : AbstractClause
    {
        public CachingAction Action { get; set; }
        public DistributedCacheEntryOptions Options { get; set; }

        public string CacheKey { get; set; }


        /// <inheritdoc />
        public override AbstractClause Clone()
        {
            return new CachingCondition
            {
                Engine = Engine,
                Action = Action,
                Options = Options,
                Component = Component,
            };
        }
    }
}
