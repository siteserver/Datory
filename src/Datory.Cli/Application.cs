using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Reflection;
using System.Threading.Tasks;
using Datory.Cli.Abstractions;
using Datory.Cli.Core;
using Datory.Cli.Tasks;
using Mono.Options;
using Quartz;
using Quartz.Impl;

namespace Datory.Cli
{
    public class Application
    {
        private bool _isHelp { get; set; }
        private string _repeat { get; set; }
        private readonly OptionSet _options;
        private static Dictionary<string, Func<IJobContext, Task>> Jobs { get; set; }
        public static string CommandName { get; private set; }
        public static string[] CommandArgs { get; private set; }
        public static ISettings Settings { get; private set; }

        public Application(ISettings settings)
        {
            Settings = settings;

            _options = new OptionSet {
                { "r|repeat=", "schedule CRON expression",
                    v => _repeat = v },
                { "h|help",  "命令说明",
                    v => _isHelp = v != null }
            };
        }

        public async Task RunAsync(string[] args)
        {
            if (!CliUtils.ParseArgs(_options, args)) return;

            var commandNames = new List<string>();
            var commandArgs = new List<string>();
            if (args.Length >= 1)
            {
                var isCommand = true;
                foreach (var arg in args)
                {
                    if (string.IsNullOrEmpty(arg)) continue;
                    
                    if (isCommand && !arg.StartsWith('-'))
                    {
                        commandNames.Add(arg.Trim());
                    }
                    else
                    {
                        isCommand = false;
                        commandArgs.Add(arg.Trim());
                    }
                }
            }
            CommandName = string.Join(" ", commandNames);
            CommandArgs = commandArgs.ToArray();

            Console.WriteLine("欢迎使用 SiteServer Cli 命令行工具");
            Console.WriteLine();

            Jobs = new Dictionary<string, Func<IJobContext, Task>>(StringComparer.CurrentCultureIgnoreCase)
            {
                {BackupJob.CommandName, BackupJob.Execute},
                {RestoreJob.CommandName, RestoreJob.Execute},
                {TestJob.CommandName, TestJob.Execute}
            };

            // PluginManager.LoadPlugins(CliUtils.PhysicalApplicationPath);
            // var pluginJobs = PluginJobManager.GetJobs();
            // if (pluginJobs != null && pluginJobs.Count > 0)
            // {
            //     foreach (var command in pluginJobs.Keys)
            //     {
            //         if (!Jobs.ContainsKey(command))
            //         {
            //             Jobs.Add(command, pluginJobs[command]);
            //         }
            //     }
            // }

            if (!Jobs.ContainsKey(CommandName))
            {
                await RunHelpAsync(_isHelp, CommandName);
            }
            else if (!string.IsNullOrEmpty(_repeat))
            {
                await RunRepeatAsync(_repeat);
            }
            else
            {
                await RunExecuteAsync(CommandName, CommandArgs, null);
            }
        }

        private async Task RunHelpAsync(bool _isHelp, string commandName)
        {
            if (_isHelp || string.IsNullOrEmpty(commandName))
            {
                var version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
                await Console.Out.WriteLineAsync($"Cli 命令行版本: {version.Substring(0, version.Length - 2)}");
                await Console.Out.WriteLineAsync($"当前文件夹: {Settings.ContentRootPath}");
                await Console.Out.WriteLineAsync($"Cli 命令行文件夹: {Assembly.GetExecutingAssembly().Location}");
                await Console.Out.WriteLineAsync();

                await CliUtils.PrintRowLine();
                await CliUtils.PrintRow("Usage");
                await CliUtils.PrintRowLine();
                BackupJob.PrintUsage();
                RestoreJob.PrintUsage();
                await CliUtils.PrintRowLine();
                await CliUtils.PrintRow("https://www.datory.io/docs/");
                await CliUtils.PrintRowLine();
                Console.ReadLine();
            }
            else
            {
                Console.WriteLine($"'{commandName}' is not a siteserver command. See 'sitserver --help'");
            }
        }

        private async Task RunRepeatAsync(string schedule)
        {
            try
            {
                var factory = new StdSchedulerFactory(new NameValueCollection
                {
                    { "quartz.serializer.type", "binary" }
                });
                var scheduler = await factory.GetScheduler();

                await scheduler.Start();

                var job = JobBuilder.Create<SchedulerJob>()
                    .WithIdentity("job1", "group1")
                    .Build();

                var trigger = TriggerBuilder.Create()
                    .WithIdentity("trigger1", "group1")
                    .StartNow()
                    .WithCronSchedule(schedule)
                    .WithPriority(1)
                    .Build();

                await scheduler.ScheduleJob(job, trigger);
                await Task.Delay(-1);
                await scheduler.Shutdown();
            }
            catch (Exception ex)
            {
                await CliUtils.PrintErrorAsync(ex.Message);
            }
        }

        public static async Task RunExecuteAsync(string commandName, string[] commandArgs, IJobExecutionContext jobContext)
        {
            try
            {
                Func<IJobContext, Task> job;
                if (Jobs.TryGetValue(commandName, out job))
                {
                    if (job != null)
                    {
                        var context = new JobContextImpl(commandName, commandArgs, jobContext);
                        await job(context);
                    }
                }
            }
            catch (Exception ex)
            {
                await CliUtils.PrintErrorAsync(ex.Message);

                var errorLogFilePath = CliUtils.CreateErrorLogFile(Settings.ContentRootPath, "datory");

                await CliUtils.AppendErrorLogsAsync(errorLogFilePath, new List<TextLogInfo>
                {
                    new TextLogInfo
                    {
                        DateTime = DateTime.Now,
                        Detail = "Console Error",
                        Exception = ex
                    }
                });
            }
        }
    }
}