using System;

namespace Datory.Tests.Utils
{
    public static class TestEnv
    {
        public static bool IsTestMachine => Environment.MachineName == "DESKTOP-PL0AATC";
    }
}
