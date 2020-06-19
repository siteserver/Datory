using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Datory.Cli.Utils
{
    public static class DirectoryUtils
    {
        public static string GetDirectoryPath(string path)
        {
            var ext = Path.GetExtension(path);
            var directoryPath = !string.IsNullOrEmpty(ext) ? Path.GetDirectoryName(path) : path;
            return directoryPath;
        }

        public static void CreateDirectoryIfNotExists(string path)
        {
            var directoryPath = GetDirectoryPath(path);

            if (!Directory.Exists(directoryPath))
            {
                try
                {
                    Directory.CreateDirectory(directoryPath);
                }
                catch
                {
                    //Scripting.FileSystemObject fso = new Scripting.FileSystemObjectClass();
                    //string[] directoryNames = directoryPath.Split('\\');
                    //string thePath = directoryNames[0];
                    //for (int i = 1; i < directoryNames.Length; i++)
                    //{
                    //    thePath = thePath + "\\" + directoryNames[i];
                    //    if (StringUtils.Contains(thePath.ToLower(), ConfigUtils.Instance.PhysicalApplicationPath.ToLower()) && !IsDirectoryExists(thePath))
                    //    {
                    //        fso.CreateFolder(thePath);
                    //    }
                    //}                    
                }
            }
        }
    }
}
