using System.Threading.Tasks;
using Datory.Cli.Core;
using Datory.Cli.Tasks;
using Datory.Tests.Utils;
using Xunit;
using Xunit.Abstractions;

namespace Datory.Cli.Tests.Tasks
{
    public class BackupJobTests : IClassFixture<UnitTestsFixture>
    {
        private readonly ITestOutputHelper _output;

        public BackupJobTests(UnitTestsFixture fixture, ITestOutputHelper output)
        {
            _output = output;
        }

        [SkippableFact]
        public async Task TestExecuteAsync()
        {
            Skip.IfNot(TestEnv.IsTestMachine);

            Assert.Equal("backup", BackupJob.CommandName);

            var context = new JobContextImpl(BackupJob.CommandName, new [] {"-d", "backup"}, null);
            await BackupJob.ExecuteAsync(context);

            Assert.Equal("backup", BackupJob.CommandName);
        }
    }
}