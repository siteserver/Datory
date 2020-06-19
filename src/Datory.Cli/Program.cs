using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Datory.Cli.Core;
using Datory.Cli.Tasks;
using Datory.Cli.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Datory.Cli
{
    internal static class Program
    {
        static async Task Main(string[] args)
        {
            try
            {
                Console.OutputEncoding = Encoding.GetEncoding(936);
            }
            catch
            {
                try
                {
                    Console.OutputEncoding = Encoding.UTF8;
                }
                catch
                {
                    // ignored
                }
            }

            var contentRootPath = Directory.GetCurrentDirectory();
            if (!File.Exists(PathUtils.Combine(contentRootPath, CliUtils.ConfigFileName)))
            {
                Settings.SaveEmptySettings(PathUtils.Combine(contentRootPath, CliUtils.ConfigFileName));
            }
            var builder = new ConfigurationBuilder()
                .SetBasePath(contentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile(CliUtils.ConfigFileName, optional: true, reloadOnChange: true)
                .AddEnvironmentVariables("DATORY_")
                .AddCommandLine(args);
            var configuration = builder.Build();

            var services = new ServiceCollection();

            services.AddSettings(configuration, contentRootPath);

            services.AddTransient<Application>();
            services.AddTransient<BackupJob>();
            services.AddTransient<RestoreJob>();
            services.AddTransient<StatusJob>();

            var provider = services.BuildServiceProvider();
            CliUtils.Provider = provider;

            var application = provider.GetService<Application>();
            await application.RunAsync(args);
        }
    }
}
