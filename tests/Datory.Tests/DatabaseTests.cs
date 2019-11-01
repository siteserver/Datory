using System;
using Datory.Tests.Utils;
using Xunit;
using Xunit.Abstractions;

namespace Datory.Tests
{
    public class DatabaseTests : IClassFixture<UnitTestsFixture>
    {
        private UnitTestsFixture _fixture { get; }
        private readonly ITestOutputHelper _output;

        public DatabaseTests(UnitTestsFixture fixture, ITestOutputHelper output)
        {
            _fixture = fixture;
            _output = output;

            _output.WriteLine(Environment.MachineName);
        }
    }
}