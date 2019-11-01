using System;

namespace Datory
{
    [AttributeUsage(AttributeTargets.Property)]
    [Serializable]
    public class DataIgnoreAttribute : Attribute
    {
    }
}
