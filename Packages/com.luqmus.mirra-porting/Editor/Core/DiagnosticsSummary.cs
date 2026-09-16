namespace Luqmus.MirraPorting.Core
{
    /// <summary>
    /// How much the scanner could not make sense of. Diagnostics are not findings: an unclosed
    /// comment in a branch nobody compiles is not the port's problem, but the report says how many
    /// there were, because a file full of them is scanned less thoroughly than it looks.
    /// </summary>
    internal readonly struct DiagnosticsSummary
    {
        internal DiagnosticsSummary(int filesWithDiagnostics, int total)
        {
            FilesWithDiagnostics = filesWithDiagnostics;
            Total = total;
        }

        internal int FilesWithDiagnostics { get; }

        internal int Total { get; }
    }
}
