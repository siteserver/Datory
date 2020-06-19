using System.IO;

namespace Datory.Cli.Utils
{
    public static class PathUtils
    {
        private static readonly char SeparatorChar = Path.DirectorySeparatorChar;

        public static string Combine(params string[] paths)
        {
            var retVal = string.Empty;
            if (paths != null && paths.Length > 0)
            {
                retVal = paths[0]?.Replace('/', SeparatorChar).TrimEnd(SeparatorChar) ?? string.Empty;
                for (var i = 1; i < paths.Length; i++)
                {
                    var path = paths[i] != null ? paths[i].Replace('/', SeparatorChar).Trim(SeparatorChar) : string.Empty;
                    retVal = Path.Combine(retVal, path);
                }
            }
            return retVal.Replace('/', SeparatorChar);
        }
    }
}
