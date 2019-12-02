using System.Collections.Generic;
using Datory.Cli.Abstractions;
using Datory.Utils;
using Microsoft.Extensions.Configuration;

namespace Datory.Cli.Core
{
    public class Settings : ISettings
    {
        private readonly IConfiguration _config;

        public Settings(IConfiguration config, string contentRootPath)
        {
            _config = config;
            ContentRootPath = contentRootPath;
            Database = new Database(Utilities.ToEnum(_config.GetValue<string>("Database:Type"), DatabaseType.MySql), _config.GetValue<string>("Database:ConnectionString"));
            Includes = Utilities.JsonDeserialize<List<string>>(_config.GetValue<string>("Tables:Includes"));
            Excludes = Utilities.JsonDeserialize<List<string>>(_config.GetValue<string>("Tables:Excludes"));
        }

        public string ContentRootPath { get; }
        public IDatabase Database { get; }
        public IList<string> Includes { get; }
        public IList<string> Excludes { get; }
    }
}
