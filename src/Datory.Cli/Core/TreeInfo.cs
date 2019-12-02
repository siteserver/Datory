using System.IO;

namespace Datory.Cli.Core
{
    public class TreeInfo
    {
        public string DirectoryPath { get; }

        public TreeInfo(string contentRootPath, string directory)
        {
            DirectoryPath = Path.Combine(contentRootPath, directory);
        }

        public string TablesFilePath => Path.Combine(DirectoryPath, "_tables.json");

        public string GetTableMetadataFilePath(string tableName)
        {
            return Path.Combine(DirectoryPath, tableName, "_metadata.json");
        }

        public string GetTableContentFilePath(string tableName, string fileName)
        {
            return Path.Combine(DirectoryPath, tableName, fileName);
        }
    }
}
