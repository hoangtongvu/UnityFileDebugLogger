using System.IO;
using UnityEngine;

namespace UnityFileDebugLogger
{
    public static class FileWriter
    {
        /// <summary>
        /// Writes text to a file.
        /// </summary>
        /// <param name="filePath">Full or relative path from Application.persistentDataPath.</param>
        /// <param name="text">The content to write.</param>
        /// <param name="append">If true, text will be appended. If false, it will overwrite.</param>
        public static void Write(string filePath, string text, bool append = true)
        {
            string fullPath = GetFullPath(filePath);

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(fullPath)); // Ensure directory exists

                if (append)
                {
                    File.AppendAllText(fullPath, text);
                }
                else
                {
                    File.WriteAllText(fullPath, text);
                }
            }
            catch (IOException e)
            {
                Debug.LogError($"FileWriter Error: Could not write to {fullPath}\n{e}");
            }
        }

        /// <summary>
        /// Returns a full file path, resolving relative paths from persistentDataPath.
        /// </summary>
        private static string GetFullPath(string filePath)
        {
            if (Path.IsPathRooted(filePath))
                return filePath;
            else
                return Path.Combine(Application.persistentDataPath, filePath);
        }

    }

}
