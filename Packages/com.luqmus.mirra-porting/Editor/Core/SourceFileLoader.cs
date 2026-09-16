using System;
using System.IO;

namespace Luqmus.MirraPorting.Core
{
    /// <summary>
    /// Reads a scanned file from disk. The only place the scanner touches file contents, so that
    /// everything above it can be tested on strings.
    /// </summary>
    internal static class SourceFileLoader
    {
        /// <summary>
        /// Reads and parses one file. A file that cannot be read is not fatal: it comes back as a
        /// <see cref="ScanError"/> and the scan goes on with the rest.
        /// </summary>
        internal static bool TryLoad(ScopedFile file, out SourceFile loaded, out ScanError error)
        {
            string text;
            try
            {
                // UTF-8 with a byte order mark is what Unity writes; ReadAllText detects it and
                // strips it. Broken bytes do not matter: everything the scanner looks for is ASCII.
                text = File.ReadAllText(file.AbsolutePath);
            }
            catch (Exception exception)
            {
                loaded = null;
                error = new ScanError(
                    file.ProjectRelativePath,
                    "Не удалось прочитать файл: " + exception.GetType().Name + ": " + exception.Message);
                return false;
            }

            loaded = SourceFile.FromText(file, text);
            error = null;
            return true;
        }
    }
}
