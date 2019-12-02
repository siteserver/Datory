using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Mono.Options;
using System.IO;
using System.Reflection;
using Datory.Utils;
using Datory.Cli.Abstractions;

namespace Datory.Cli.Core
{
    public static class CliUtils
    {
        public const int PageSize = 500;

        public static ServiceProvider Provider { get; set; }

        private const int ConsoleTableWidth = 77;

        private static string AlignCentre(string text, int width)
        {
            text = text.Length > width ? text.Substring(0, width - 3) + "..." : text;

            return string.IsNullOrEmpty(text)
                ? new string(' ', width)
                : text.PadRight(width - (width - text.Length) / 2).PadLeft(width);
        }

        // https://stackoverflow.com/questions/491595/best-way-to-parse-command-line-arguments-in-c
        public static bool ParseArgs(OptionSet options, string[] args)
        {
            try
            {
                options.Parse(args);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static async Task PrintRowLineAsync()
        {
            await Console.Out.WriteLineAsync(new string('-', ConsoleTableWidth));
        }

        public static async Task PrintRowAsync(params string[] columns)
        {
            int width = (ConsoleTableWidth - columns.Length) / columns.Length;
            string row = "|";

            foreach (string column in columns)
            {
                row += AlignCentre(column, width) + "|";
            }

            await Console.Out.WriteLineAsync(row);
        }

        public static async Task PrintErrorAsync(string errorMessage)
        {
            await Console.Out.WriteLineAsync();
            await Console.Out.WriteLineAsync(errorMessage);
        }

        public static async Task PrintRowLine()
        {
            await Console.Out.WriteLineAsync(new string('-', ConsoleTableWidth));
        }

        public static async Task PrintRow(params string[] columns)
        {
            int width = (ConsoleTableWidth - columns.Length) / columns.Length;
            string row = "|";

            foreach (string column in columns)
            {
                row += AlignCentre(column, width) + "|";
            }

            await Console.Out.WriteLineAsync(row);
        }

        public static async Task PrintError(string errorMessage)
        {
            await Console.Out.WriteLineAsync();
            await Console.Out.WriteLineAsync(errorMessage);
        }

        public static string CreateErrorLogFile(string contentRootPath, string commandName)
        {
            var filePath = Path.Combine(contentRootPath, $"{commandName}.error.log");
            if (FileExists(filePath))
            {
                File.Delete(filePath);
            }
            return filePath;
        }

        public static async Task WriteAllTextAsync(string path, string contents)
        {
            await File.WriteAllTextAsync(path, contents, Encoding.UTF8);
        }

        public static async Task AppendAllTextAsync(string path, string contents)
        {
            await File.AppendAllTextAsync(path, contents, Encoding.UTF8);
        }

        public static async Task<string> ReadAllTextAsync(string path)
        {
            return await File.ReadAllTextAsync(path, Encoding.UTF8);
        }

        public static bool FileExists(string path)
        {
            return File.Exists(path);
        }

        public static async Task AppendErrorLogsAsync(string filePath, List<TextLogInfo> logs)
        {
            if (logs == null || logs.Count <= 0) return;

            if (!FileExists(filePath))
            {
                await WriteAllTextAsync(filePath, string.Empty);
            }

            var builder = new StringBuilder();

            foreach (var log in logs)
            {
                builder.AppendLine();
                builder.Append(log);
                builder.AppendLine();
            }

            await AppendAllTextAsync(filePath, builder.ToString());
        }

        public static async Task AppendErrorLogAsync(string filePath, TextLogInfo log)
        {
            if (log == null) return;

            if (!FileExists(filePath))
            {
                await WriteAllTextAsync(filePath, string.Empty);
            }

            var builder = new StringBuilder();

            builder.AppendLine();
            builder.Append(log);
            builder.AppendLine();

            await AppendAllTextAsync(filePath, builder.ToString());
        }

        public static async Task<(bool IsConnectionWorks, string ErrorMessage)> CheckSettingsAsync(ISettings settings)
        {
            if (string.IsNullOrEmpty(settings.Database.ConnectionString))
            {
                return (false, "请在config.json文件中设置数据库类型与连接字符串");
            }

            return await settings.Database.IsConnectionWorksAsync();
        }

        private static string GetConfigFilePath(string contentRootPath, string configFile)
        {
            return FileExists(configFile)
                ? configFile
                : Path.Combine(contentRootPath,
                    !string.IsNullOrEmpty(configFile) ? configFile : "Web.config");
        }
    }
}
