using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Datory.Cli.Core;
using Datory.Cli.Tasks;
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

            try
            {
                var contentRootPath = Directory.GetCurrentDirectory();
                var builder = new ConfigurationBuilder()
                    .SetBasePath(contentRootPath)
                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                IConfigurationRoot configuration = builder.Build();

                var services = new ServiceCollection();

                services.AddSettings(configuration, contentRootPath);

                services.AddTransient<Application>();
                services.AddTransient<BackupJob>();
                services.AddTransient<RestoreJob>();
                services.AddTransient<TestJob>();
                services.AddTransient<VersionJob>();

                var provider = services.BuildServiceProvider();
                CliUtils.Provider = provider;

                var application = provider.GetService<Application>();
                await application.RunAsync(args);
            }
            finally
            {
                Console.WriteLine("\r\nPress any key to exit...");
                Console.ReadKey();
            }
        }


    }
}
