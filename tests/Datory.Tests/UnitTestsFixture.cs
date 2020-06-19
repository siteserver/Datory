using System.IO;
using System.Threading.Tasks;
using Datory.Cli;
using Datory.Cli.Abstractions;
using Datory.Cli.Core;
using Datory.Cli.Tasks;
using Datory.Cli.Utils;
using Datory.Tests.Mocks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Datory.Tests
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

            var services = new ServiceCollection();

            services.AddSettings(config, contentRootPath);

            services.AddTransient<Application>();
            services.AddTransient<BackupJob>();
            services.AddTransient<RestoreJob>();
            services.AddTransient<StatusJob>();

            var provider = services.BuildServiceProvider();
            CliUtils.Provider = provider;
        }

        public async Task InitializeAsync()
        {
            var repository = new Repository<TestTableInfo>(Database);
            var isExists = await Database.IsTableExistsAsync(repository.TableName);
            if (!isExists)
            {
                await Database.CreateTableAsync(repository.TableName, repository.TableColumns);
            }
        }

        public Task DisposeAsync()
        {
            return Task.CompletedTask;
        }
    }
}
