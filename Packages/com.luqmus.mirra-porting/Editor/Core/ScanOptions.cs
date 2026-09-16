namespace Luqmus.MirraPorting.Core
{
    /// <summary>
    /// How to run a scan. The menu path fills in nothing but the folder: it has no progress to
    /// report to anybody and nothing to cancel it.
    /// </summary>
    internal sealed class ScanOptions
    {
        internal ScanOptions(string outputDirectory)
        {
            OutputDirectory = outputDirectory;
        }

        /// <summary>Where report.json and report.md go.</summary>
        internal string OutputDirectory { get; }

        /// <summary>Called between files and between checks. May be null.</summary>
        internal ScanProgressHandler OnProgress { get; set; }

        /// <summary>Asked between files and between checks. May be null.</summary>
        internal ScanCancellationCheck IsCancelled { get; set; }
    }
}
