using System;
using System.Reflection;
using System.Threading.Tasks;
using Datory.Cli.Abstractions;
using Datory.Cli.Core;
using Microsoft.Extensions.DependencyInjection;
using Mono.Options;

namespace Datory.Cli.Tasks
{
    public class TestJob
    {
        public const string CommandName = "test add";

        public static async Task Execute(IJobContext context)
        {
            var application = CliUtils.Provider.GetService<TestJob>();
            await application.RunAsync(context);
        }

        private bool _isHelp;
        private string _configFileName;
        private readonly OptionSet _options;
        private readonly ISettings _settings;

        public TestJob(ISettings settings)
        {
            _options = new OptionSet()
            {
                {
                    "c|config=", "the {web.config} file name.",
                    v => _configFileName = v
                },
                {
                    "h|help", "命令说明",
                    v => _isHelp = v != null
                }
            };
        }

        public async Task RunAsync(IJobContext context)
        {
            if (!CliUtils.ParseArgs(_options, context.Args)) return;

            if (_isHelp)
            {
                return;
            }

            var version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            await Console.Out.WriteLineAsync($"SiteServer CLI Version: {version.Substring(0, version.Length - 2)}");
            await Console.Out.WriteLineAsync($"Work Directory: {_settings.ContentRootPath}");
            await Console.Out.WriteLineAsync($"siteserver.exe Path: {Assembly.GetExecutingAssembly().Location}");
            await Console.Out.WriteLineAsync();
        }
    }
}
