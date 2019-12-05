using Datory.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Datory.Caching
{
    public enum CachingAction
    {
        Get,
        Set,
        Remove
    }
}