using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using Datory.Cli.Abstractions;
using Datory.Cli.Core;
using Microsoft.Extensions.DependencyInjection;
using Mono.Options;
using System.IO;
using Datory.Cli.Utils;

namespace Datory.Cli.Tasks
{
    public class VersionJob
    {
        public const string CommandName = "version";

        public static async Task Execute(IJobContext context)
        {
            var application = CliUtils.Provider.GetService<VersionJob>();
            await application.RunAsync(context);
        }

        public static void PrintUsage()
        {
            Console.WriteLine("显示当前版本: datory-cli version");
            var job = new VersionJob(null);
            job._options.WriteOptionDescriptions(Console.Out);
            Console.WriteLine();
        }
        
        private bool _isHelp;
        private readonly OptionSet _options;
        private readonly ISettings _settings;

        public VersionJob(ISettings settings)
        {
            _settings = settings;
            _options = new OptionSet {
                { "h|help",  "命令说明",
                    v => _isHelp = v != null }
            };
        }

        public async Task RunAsync(IJobContext context)
        {
            if (!CliUtils.ParseArgs(_options, context.Args)) return;

            if (_isHelp)
            {
                PrintUsage();
                return;
            }

            var version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            await Console.Out.WriteLineAsync($"SiteServer CLI 版本号: {version.Substring(0, version.Length - 2)}");
            await Console.Out.WriteLineAsync($"当前文件夹: {_settings.ContentRootPath}");
            await Console.Out.WriteLineAsync();

            var (isConnectionWorks, errorMessage) = await CliUtils.CheckSettingsAsync(_settings);
            if (!isConnectionWorks)
            {
                try
                {
                    var cmsVersion = FileVersionInfo.GetVersionInfo(Path.Combine(_settings.ContentRootPath, "Bin", "SiteServer.CMS.dll")).ProductVersion;
                    await Console.Out.WriteLineAsync($"SitServer CMS Version: {cmsVersion}");
                }
                catch
                {
                    // ignored
                }

                await Console.Out.WriteLineAsync($"数据库类型: {_settings.Database.DatabaseType.GetValue()}");
                await Console.Out.WriteLineAsync($"连接字符串: {_settings.Database.ConnectionString}");
            }
        }
    }
}
