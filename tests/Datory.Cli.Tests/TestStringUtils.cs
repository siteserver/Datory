using Datory.Utils;
using Xunit;

namespace Datory.Cli.Tests
{
    public class TestStringUtils
    {
        [Fact]
        public void TestReplaceEndsWith()
        {
            var replaced = Utilities.ReplaceEndsWith("UserName DESC", " DESC", string.Empty);
            Assert.Equal("UserName", replaced);
        }

        [Fact]
        public void TestReplaceEndsWithIgnoreCase()
        {
            var replaced = Utilities.ReplaceEndsWithIgnoreCase("UserName desc", " DESC", string.Empty);
            Assert.Equal("UserName", replaced);
        }
    }
}
