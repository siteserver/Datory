using System.IO;
using System.Threading.Tasks;
using Datory.Cli.Abstractions;
using Datory.Cli.Core;
using Datory.Tests.Mocks;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Datory.Tests.Utils
{
    // https://mderriey.com/2017/09/04/async-lifetime-with-xunit/
    public class UnitTestsFixture : IAsyncLifetime
    {
        public IDatabase Database => _settings.Database;
        private readonly ISettings _settings;

        public UnitTestsFixture()
        {
            var contentRootPath = Directory.GetCurrentDirectory();

            var config = new ConfigurationBuilder()
                .SetBasePath(contentRootPath)
                .AddJsonFile("config.json")
                .Build();

            _settings = new Settings(config, contentRootPath);
        }

        public async Task InitializeAsync()
        {
            if (!TestEnv.IsTestMachine) return;

            var repository = new Repository<TestTableInfo>(Database);
            var isExists = await Database.IsTableExistsAsync(repository.TableName);
            if (isExists)
            {
                await Database.DropTableAsync(repository.TableName);
            }

            await Database.CreateTableAsync(repository.TableName, repository.TableColumns);
        }

        public async Task DisposeAsync()
        {
            var tableNames = await Database.GetTableNamesAsync();
            foreach (var tableName in tableNames)
            {
                await Database.DropTableAsync(tableName);
            }
        }
    }
}
