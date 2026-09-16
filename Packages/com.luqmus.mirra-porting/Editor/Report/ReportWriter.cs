using System.IO;
using System.Text;

namespace Luqmus.MirraPorting.Report
{
    /// <summary>
    /// Writes the report files. Every file is written to a temporary neighbour first and only then
    /// put in place, so a reader never sees half a report, and a crash never destroys the previous
    /// one.
    /// </summary>
    internal static class ReportWriter
    {
        internal const string JsonFileName = "report.json";
        internal const string MarkdownFileName = "report.md";

        private const string TemporarySuffix = ".tmp";

        /// <summary>Creates the folder and clears temporary files a previous run may have left.</summary>
        internal static void PrepareDirectory(string directory)
        {
            Directory.CreateDirectory(directory);

            foreach (string leftover in Directory.GetFiles(directory, "*" + TemporarySuffix))
            {
                File.Delete(leftover);
            }
        }

        /// <summary>
        /// Writes one file in place of whatever was there. UTF-8 without a byte order mark: the
        /// report is read by tools as often as by people.
        /// </summary>
        internal static void WriteText(string path, string text)
        {
            string temporary = path + TemporarySuffix;
            File.WriteAllText(temporary, text, new UTF8Encoding(false));

            if (File.Exists(path))
            {
                // Replace swaps the files in one step. Move cannot overwrite in this API profile.
                File.Replace(temporary, path, null);
            }
            else
            {
                File.Move(temporary, path);
            }
        }
    }
}
