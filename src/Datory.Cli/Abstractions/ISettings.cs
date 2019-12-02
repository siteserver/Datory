using System.Collections.Generic;

namespace Datory.Cli.Abstractions
{
    public interface ISettings
    {
        string ContentRootPath { get; }

        IDatabase Database { get; }

        IList<string> Includes { get; }

        IList<string> Excludes { get; }
    }
}