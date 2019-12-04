using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Datory.Cli.Abstractions;
using Datory.Cli.Core;
using Microsoft.Extensions.DependencyInjection;
using Mono.Options;
using Datory.Utils;

namespace Datory.Cli.Tasks
{
    public class BackupJob
    {
        public const string CommandName = "backup";

        public static async Task ExecuteAsync(IJobContext context)
        {
            var application = CliUtils.Provider.GetService<BackupJob>();
            await application.RunAsync(context);
        }

        public static void PrintUsage()
        {
            Console.WriteLine("数据库备份: datory-cli backup");
            var job = new BackupJob(null);
            job._options.WriteOptionDescriptions(Console.Out);
            Console.WriteLine();
        }

        private string _directory;
        private int _maxRows;
        private bool _isHelp;
        private readonly OptionSet _options;
        private readonly ISettings _settings;

        public BackupJob(ISettings settings)
        {
            _settings = settings;
            _options = new OptionSet() {
                { "d|directory=", "指定保存备份文件的文件夹名称",
                    v => _directory = v },
                { "max-rows=", "指定需要备份的表的最大行数",
                    v => _maxRows = v == null ? 0 : Convert.ToInt32(v) },
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

            var directory = _directory;
            if (string.IsNullOrEmpty(directory))
            {
                directory = $"backup/{DateTime.Now:yyyy-MM-dd}";
            }

            var treeInfo = new TreeInfo(_settings.ContentRootPath, directory);
            if (!Directory.Exists(treeInfo.DirectoryPath))
            {
                Directory.CreateDirectory(treeInfo.DirectoryPath);
            }

            var (isConnectionWorks, errorMessage) = await CliUtils.CheckSettingsAsync(_settings);
            if (!isConnectionWorks)
            {
                await CliUtils.PrintErrorAsync(errorMessage);
                return;
            }

            await Console.Out.WriteLineAsync($"数据库类型: {_settings.Database.DatabaseType.GetDisplayName()}");
            await Console.Out.WriteLineAsync($"连接字符串: {_settings.Database.ConnectionString}");
            await Console.Out.WriteLineAsync($"备份文件夹: {treeInfo.DirectoryPath}");

            var includes = new List<string>(_settings.Includes);
            var excludes = new List<string>(_settings.Excludes);
            excludes.Add("bairong_Log");
            excludes.Add("bairong_ErrorLog");
            excludes.Add("siteserver_ErrorLog");
            excludes.Add("siteserver_Log");
            excludes.Add("siteserver_Tracking");

            var allTableNames = await _settings.Database.GetTableNamesAsync();
            var tableNames = new List<string>();

            foreach (var tableName in allTableNames)
            {
                if (!Utilities.ContainsIgnoreCase(includes, tableName)) continue;
                if (Utilities.ContainsIgnoreCase(excludes, tableName)) continue;
                if (Utilities.ContainsIgnoreCase(tableNames, tableName)) continue;
                tableNames.Add(tableName);
            }

            await CliUtils.WriteAllTextAsync(treeInfo.TablesFilePath, Utilities.JsonSerialize(tableNames));

            await CliUtils.PrintRowLineAsync();
            await CliUtils.PrintRowAsync("备份表名称", "总条数");
            await CliUtils.PrintRowLineAsync();

            foreach (var tableName in tableNames)
            {
                var repository = new Repository(_settings.Database, tableName);
                var tableInfo = new TableInfo
                {
                    Columns = await _settings.Database.GetTableColumnsAsync(tableName),
                    TotalCount = await repository.CountAsync(),
                    RowFiles = new List<string>()
                };

                if (_maxRows > 0 && tableInfo.TotalCount > _maxRows)
                {
                    tableInfo.TotalCount = _maxRows;
                }

                await CliUtils.PrintRowAsync(tableName, tableInfo.TotalCount.ToString("#,0"));

                var identityColumnName = await _settings.Database.AddIdentityColumnIdIfNotExistsAsync(tableName, tableInfo.Columns);

                if (tableInfo.TotalCount > 0)
                {
                    var current = 1;
                    if (tableInfo.TotalCount > CliUtils.PageSize)
                    {
                        var pageCount = (int)Math.Ceiling((double)tableInfo.TotalCount / CliUtils.PageSize);

                        using (var progress = new ProgressBar())
                        {
                            for (; current <= pageCount; current++)
                            {
                                progress.Report((double)(current - 1) / pageCount);

                                var fileName = $"{current}.json";
                                tableInfo.RowFiles.Add(fileName);
                                var offset = (current - 1) * CliUtils.PageSize;
                                var limit = tableInfo.TotalCount - offset < CliUtils.PageSize ? tableInfo.TotalCount - offset : CliUtils.PageSize;

                                var rows = await repository.GetAllAsync<IEnumerable<dynamic>>(Q.Offset(offset).Limit(limit).OrderBy(identityColumnName));

                                await CliUtils.WriteAllTextAsync(treeInfo.GetTableContentFilePath(tableName, fileName), Utilities.JsonSerialize(rows));
                            }
                        }
                    }
                    else
                    {
                        var fileName = $"{current}.json";
                        tableInfo.RowFiles.Add(fileName);
                        var rows = await repository.GetAllAsync<IEnumerable<dynamic>>(Q.OrderBy(identityColumnName));

                        await CliUtils.WriteAllTextAsync(treeInfo.GetTableContentFilePath(tableName, fileName), Utilities.JsonSerialize(rows));
                    }
                }

                await CliUtils.WriteAllTextAsync(treeInfo.GetTableMetadataFilePath(tableName), Utilities.JsonSerialize(tableInfo));
            }

            await CliUtils.PrintRowLineAsync();
            await Console.Out.WriteLineAsync($"恭喜，成功备份数据库至文件夹：{treeInfo.DirectoryPath}！");
        }
    }
}
