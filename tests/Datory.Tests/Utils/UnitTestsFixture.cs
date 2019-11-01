using System.IO;
using Microsoft.Extensions.Configuration;

namespace Datory.Tests.Utils
{
    public class UnitTestsFixture
    {
        public IConfigurationRoot Config { get; set; }

        public UnitTestsFixture()
        {
            var contentRootPath = Directory.GetCurrentDirectory();

            var config = new ConfigurationBuilder()
                .SetBasePath(contentRootPath)
                .AddJsonFile("ss.json")
                .Build();

            Config = config;
        }
    }
}
