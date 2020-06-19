using Datory.Cli.Utils;

namespace Datory.Cli.Core
{
    public class TreeInfo
    {
        public string DirectoryPath { get; }

        public TreeInfo(string contentRootPath, string directory)
        {
            DirectoryPath = PathUtils.Combine(contentRootPath, directory);
        }

        public string TablesFilePath => PathUtils.Combine(DirectoryPath, "_tables.json");

        public void CreateTableDirectoryPath(string tableName)
        {
            var path = PathUtils.Combine(DirectoryPath, tableName);
            DirectoryUtils.CreateDirectoryIfNotExists(path);
        }

        public string GetTableMetadataFilePath(string tableName)
        {
            return PathUtils.Combine(DirectoryPath, tableName, "_metadata.json");
        }

        public string GetTableContentFilePath(string tableName, string fileName)
        {
            return PathUtils.Combine(DirectoryPath, tableName, fileName);
        }
    }
}
