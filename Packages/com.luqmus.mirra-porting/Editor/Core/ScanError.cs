namespace Luqmus.MirraPorting.Core
{
    /// <summary>
    /// Something the scanner could not do. Collected instead of thrown: a folder it cannot read or
    /// a file it cannot parse must not stop the scan. The runner turns these into
    /// SCAN.INTERNAL_ERROR findings.
    /// </summary>
    internal sealed class ScanError
    {
        internal ScanError(string path, string message)
        {
            Path = path ?? string.Empty;
            Message = message ?? string.Empty;
        }

        /// <summary>Project relative path of whatever failed, or an empty string.</summary>
        internal string Path { get; }

        /// <summary>What went wrong, in Russian, including the exception text.</summary>
        internal string Message { get; }
    }
}
