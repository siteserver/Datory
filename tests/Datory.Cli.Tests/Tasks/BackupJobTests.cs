using Datory.Cli.Tasks;
using Xunit;

namespace Datory.Cli.Tests.Tasks
{
    public class BackupJobTests
    {
        [Fact]
        public void TestReplaceEndsWith()
        {
            Assert.Equal("backup", BackupJob.CommandName);
        }
    }
}