using System;

namespace Luqmus.MirraPorting.Core
{
    /// <summary>
    /// Path helpers for the one path shape the scanner uses everywhere: project relative, forward
    /// slashes, no trailing slash. It is the shape CompilationPipeline reports source files in,
    /// so the two can be compared directly.
    /// </summary>
    internal static class PathUtil
    {
        /// <summary>Replaces backslashes with forward slashes and drops a trailing slash.</summary>
        internal static string Normalize(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return string.Empty;
            }

            string normalized = path.Replace('\\', '/');
            while (normalized.Length > 1 && normalized[normalized.Length - 1] == '/')
            {
                normalized = normalized.Substring(0, normalized.Length - 1);
            }

            return normalized;
        }

        /// <summary>Joins two already normalized parts with a single forward slash.</summary>
        internal static string Join(string left, string right)
        {
            if (string.IsNullOrEmpty(left))
            {
                return right ?? string.Empty;
            }

            if (string.IsNullOrEmpty(right))
            {
                return left;
            }

            return left + "/" + right;
        }

        /// <summary>
        /// Unity does not import directories whose name starts with a dot or ends with a tilde,
        /// so neither does the scanner.
        /// </summary>
        internal static bool IsHiddenDirectoryName(string directoryName)
        {
            if (string.IsNullOrEmpty(directoryName))
            {
                return false;
            }

            return directoryName[0] == '.' || directoryName[directoryName.Length - 1] == '~';
        }

        /// <summary>
        /// True when the path has the given whole segment. Case insensitive, like every other path
        /// comparison here. "Assets/Editorial/A.cs" does not contain the segment "Editor".
        /// </summary>
        internal static bool HasSegment(string path, string segment)
        {
            if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(segment))
            {
                return false;
            }

            int start = 0;
            while (start <= path.Length)
            {
                int end = path.IndexOf('/', start);
                if (end < 0)
                {
                    end = path.Length;
                }

                int length = end - start;
                if (length == segment.Length &&
                    string.Compare(path, start, segment, 0, length, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    return true;
                }

                start = end + 1;
            }

            return false;
        }

        /// <summary>
        /// Rebases an absolute file path onto the project relative path of its root. A local
        /// package resolved outside the project still reports as "Packages/&lt;name&gt;/...", which is
        /// how the project addresses it. Returns null when the file is not under the root.
        /// </summary>
        internal static string ToProjectRelative(ScanRoot root, string absolutePath)
        {
            string normalized = Normalize(absolutePath);
            string rootPath = Normalize(root.AbsolutePath);
            if (normalized.Length == 0 || rootPath.Length == 0)
            {
                return null;
            }

            if (!normalized.StartsWith(rootPath, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            if (normalized.Length == rootPath.Length)
            {
                return root.ProjectRelativePath;
            }

            if (normalized[rootPath.Length] != '/')
            {
                return null;
            }

            return Join(root.ProjectRelativePath, normalized.Substring(rootPath.Length + 1));
        }
    }
}
