using System.Collections.Generic;
using System.IO;
using Datory.Cli.Abstractions;
using Datory.Utils;
using Microsoft.Extensions.Configuration;

namespace Datory.Cli.Core
{
    public class Settings : ISettings
    {
        private readonly IConfiguration _config;

        public Settings()
        {

        }

        public Settings(IConfiguration config, string contentRootPath)
        {
            _config = config;
            ContentRootPath = contentRootPath;
            Database = new Database(Utilities.ToEnum(_config.GetValue<string>("Database:Type"), DatabaseType.MySql), _config.GetValue<string>("Database:ConnectionString"));
            Includes = Utilities.JsonDeserialize<List<string>>(_config.GetValue<string>("Tables:Includes"));
            Excludes = Utilities.JsonDeserialize<List<string>>(_config.GetValue<string>("Tables:Excludes"));

            if (Includes == null) {
                Includes = new List<string>();
            }
            if (Excludes == null) {
                Excludes = new List<string>();
            }
        }

        public string ContentRootPath { get; }
        public IDatabase Database { get; }
        public IList<string> Includes { get; }
        public IList<string> Excludes { get; }

        public static void SaveEmptySettings(string filePath)
        {
            File.WriteAllText(filePath, @"{
  ""database"": {
    ""type"": ""MySql"",
    ""connectionString"": null
  },
  ""includes"": [],
  ""excludes"": []
}
");
        }
    }
}
