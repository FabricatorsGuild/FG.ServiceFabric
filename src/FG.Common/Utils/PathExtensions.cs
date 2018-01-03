namespace FG.Common.Utils
{
    using System;
    using System.IO;
    using System.Text;

    public static class PathExtensions
    {
        private static readonly char[] DirectorySeparatorCharArray = { Path.DirectorySeparatorChar };

        public static string GetAbsolutePath(string relativePath)
        {
            var currentDirectory = Directory.GetCurrentDirectory();
            return GetAbsolutePath(currentDirectory, relativePath);
        }

        public static string GetAbsolutePath(string basePath, string relativePath)
        {
            var combinedPath = relativePath;
            if (!Path.IsPathRooted(relativePath))
            {
                combinedPath = Path.Combine(basePath, relativePath);
            }

            return Path.GetFullPath(new Uri(combinedPath).LocalPath);
        }

        public static string GetEvaluatedPath(string path)
        {
            if (!Path.IsPathRooted(path))
            {
                return GetAbsolutePath(path);
            }

            return Path.GetFullPath(new Uri(path).LocalPath);
        }

        public static string MakeRelativePath(string basePath, string path)
        {
            path = path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            basePath = basePath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            var pathCommonToBase = path.RemoveCommonPrefix(basePath, Path.DirectorySeparatorChar);
            var commonPrefix = path.RemoveFromEnd(pathCommonToBase);
            if (commonPrefix.EndsWith(Path.DirectorySeparatorChar.ToString()))
            {
                commonPrefix = commonPrefix.RemoveFromEnd(Path.DirectorySeparatorChar.ToString());
            }

            var basePathFromCommonPrefix = basePath.RemoveFromStart(commonPrefix);
            var backTrailingComponents = basePathFromCommonPrefix.Split(DirectorySeparatorCharArray, StringSplitOptions.RemoveEmptyEntries);

            var builder = new StringBuilder();
            foreach (var backTrailingComponent in backTrailingComponents)
            {
                builder.Append("..");
                builder.Append(Path.DirectorySeparatorChar);
            }

            builder.Append(pathCommonToBase);

            return builder.ToString();
        }

        /// <summary>
        ///     Creates a relative path from one file or folder to another.
        /// </summary>
        /// <param name="fromPath">Contains the directory that defines the start of the relative path.</param>
        /// <param name="toPath">Contains the path that defines the endpoint of the relative path.</param>
        /// <returns>The relative path from the start directory to the end path or <c>toPath</c> if the paths are not related.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="UriFormatException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public static string MakeRelativePathX(string fromPath, string toPath)
        {
            if (string.IsNullOrEmpty(fromPath))
            {
                throw new ArgumentNullException(nameof(fromPath));
            }

            if (string.IsNullOrEmpty(toPath))
            {
                throw new ArgumentNullException(nameof(toPath));
            }

            var fromUri = new Uri(fromPath);
            var toUri = new Uri(toPath);

            if (fromUri.Scheme != toUri.Scheme)
            {
                return toPath;
            }
 // path can't be made relative.

            var relativeUri = fromUri.MakeRelativeUri(toUri);
            var relativePath = Uri.UnescapeDataString(relativeUri.ToString());

            if (toUri.Scheme.Equals("file", StringComparison.InvariantCultureIgnoreCase))
            {
                relativePath = relativePath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            }

            return relativePath;
        }
    }
}