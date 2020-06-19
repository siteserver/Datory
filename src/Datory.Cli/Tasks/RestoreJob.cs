using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Datory.Cli.Abstractions;
using Datory.Cli.Core;
using Datory.Cli.Utils;
using Datory.Utils;
using Microsoft.Extensions.DependencyInjection;
using Mono.Options;
using Newtonsoft.Json.Linq;

namespace Datory.Cli.Tasks
{
    public class RestoreJob
    {
        public const string CommandName = "restore";

        public static async Task ExecuteAsync(IJobContext context)
        {
            var application = CliUtils.Provider.GetService<RestoreJob>();
            await application.RunAsync(context);
        }

        public static void PrintUsage()
        {
            Console.WriteLine("数据库恢复: datory-cli restore");
            var job = new RestoreJob(null);
            job._options.WriteOptionDescriptions(Console.Out);
            Console.WriteLine();
        }

        private string _directory;
        private bool _isHelp;
        private readonly OptionSet _options;
        private readonly ISettings _settings;

        public RestoreJob(ISettings settings)
        {
            _settings = settings;

            _options = new OptionSet {
                { "d|directory=", "从指定的文件夹中恢复数据",
                    v => _directory = v },
                { "h|help",  "命令说明",
                    v => _isHelp = v != null }
            };
        }

        private async Task RunAsync(IJobContext context)
        {
            if (!CliUtils.ParseArgs(_options, context.Args)) return;

            if (_isHelp)
            {
                PrintUsage();
                return;
            }

            if (string.IsNullOrEmpty(_directory))
            {
                await CliUtils.PrintErrorAsync("需要指定恢复数据的文件夹名称：directory");
                return;
            }

            var treeInfo = new TreeInfo(_settings.ContentRootPath, _directory);

            if (!Directory.Exists(treeInfo.DirectoryPath))
            {
                await CliUtils.PrintErrorAsync($"恢复数据的文件夹 {treeInfo.DirectoryPath} 不存在");
                return;
            }

            var tablesFilePath = treeInfo.TablesFilePath;
            if (!CliUtils.FileExists(tablesFilePath))
            {
                await CliUtils.PrintErrorAsync($"恢复文件 {treeInfo.TablesFilePath} 不存在");
                return;
            }

            var (isConnectionWorks, errorMessage) = await CliUtils.CheckSettingsAsync(_settings);
            if (!isConnectionWorks)
            {
                await CliUtils.PrintErrorAsync(errorMessage);
                return;
            }

            await Console.Out.WriteLineAsync($"数据库类型: {_settings.Database.DatabaseType.GetDisplayName()}");
            await Console.Out.WriteLineAsync($"连接字符串: {_settings.Database.ConnectionString}");
            await Console.Out.WriteLineAsync($"恢复文件夹: {treeInfo.DirectoryPath}");

            var tableNames = Utilities.JsonDeserialize<List<string>>(await CliUtils.ReadAllTextAsync(tablesFilePath));

            await CliUtils.PrintRowLineAsync();
            await CliUtils.PrintRowAsync("恢复表名称", "总条数");
            await CliUtils.PrintRowLineAsync();

            var includes = new List<string>(_settings.Includes);
            var excludes = new List<string>(_settings.Excludes);
            var errorLogFilePath = CliUtils.CreateErrorLogFile(_settings.ContentRootPath, CommandName);

            foreach (var tableName in tableNames)
            {
                try
                {
                    

                    if (includes.Count > 0 && !Utilities.ContainsIgnoreCase(includes, tableName)) continue;
                    if (excludes.Count > 0 && Utilities.ContainsIgnoreCase(excludes, tableName)) continue;

                    var metadataFilePath = treeInfo.GetTableMetadataFilePath(tableName);

                    if (!CliUtils.FileExists(metadataFilePath)) continue;

                    var tableInfo = Utilities.JsonDeserialize<TableInfo>(await CliUtils.ReadAllTextAsync(metadataFilePath));

                    var repository = new Repository(_settings.Database, tableName, tableInfo.Columns);

                    await CliUtils.PrintRowAsync(tableName, tableInfo.TotalCount.ToString("#,0"));

                    if (!await _settings.Database.IsTableExistsAsync(tableName))
                    {
                        try
                        {
                            await _settings.Database.CreateTableAsync(tableName, tableInfo.Columns);
                        }
                        catch (Exception ex)
                        {
                            await CliUtils.AppendErrorLogAsync(errorLogFilePath, new TextLogInfo
                            {
                                DateTime = DateTime.Now,
                                Detail = $"创建表 {tableName}",
                                Exception = ex
                            });

                            continue;
                        }
                    }
                    else
                    {
                        await _settings.Database.AlterTableAsync(tableName, tableInfo.Columns);
                    }

                    if (tableInfo.RowFiles.Count > 0)
                    {
                        using (var progress = new ProgressBar())
                        {
                            for (var i = 0; i < tableInfo.RowFiles.Count; i++)
                            {
                                progress.Report((double)i / tableInfo.RowFiles.Count);

                                var fileName = tableInfo.RowFiles[i];

                                var objects = Utilities.JsonDeserialize<List<JObject>>(
                                    await CliUtils.ReadAllTextAsync(treeInfo.GetTableContentFilePath(tableName, fileName)));

                                try
                                {
                                    await repository.BulkInsertAsync(objects);
                                }
                                catch (Exception ex)
                                {
                                    await CliUtils.AppendErrorLogAsync(errorLogFilePath, new TextLogInfo
                                    {
                                        DateTime = DateTime.Now,
                                        Detail = $"插入表 {tableName}, 文件名 {fileName}",
                                        Exception = ex
                                    });
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    await CliUtils.AppendErrorLogAsync(errorLogFilePath, new TextLogInfo
                    {
                        DateTime = DateTime.Now,
                        Detail = $"插入表 {tableName}",
                        Exception = ex
                    });
                }
            }

            await CliUtils.PrintRowLineAsync();

            await Console.Out.WriteLineAsync($"恭喜，成功从文件夹：{treeInfo.DirectoryPath} 恢复数据！");
        }
    }
}
