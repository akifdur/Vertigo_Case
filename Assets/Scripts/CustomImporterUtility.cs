using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace CustomImporter
{
    public static class CustomImporterUtility
    {
        public static List<string> FindFiles(string path)
        {
            if (Directory.Exists(path) == false)
                return null;

            var files = new List<string>();
            FindFilesRecursively(path, ref files);
            return files;
        }
        public static List<string> FindFiles(string path, Predicate<string> predicate)
        {
            if (Directory.Exists(path) == false)
                return null;

            var files = new List<string>();
            FindFilesRecursively(path, ref files, predicate);
            return files;
        }
        private static void FindFilesRecursively(string path, ref List<string> files)
        {
            foreach (var d in Directory.GetDirectories(path))
            {
                files.AddRange(Directory.GetFiles(d));
                FindFilesRecursively(d, ref files);
            }
        }
        private static void FindFilesRecursively(string path, ref List<string> files, Predicate<string> predicate)
        {
            foreach (var d in Directory.GetDirectories(path))
            {
                foreach (var f in Directory.GetFiles(d))
                {
                    if (predicate.Invoke(f))
                    {
                        files.Add(f);
                    }
                }
                FindFilesRecursively(d, ref files, predicate);
            }
        }

        public static string GetGuidExpression(string guid)
        {
            return $"guid: {guid}";
        }
        public static string ConvertToAbsolutePath(string assetPath)
        {
            string projectPath = Application.dataPath;
            projectPath = projectPath.Substring(0, projectPath.Length - 6);
            return Path.Combine(projectPath, assetPath);
        }
    }
}