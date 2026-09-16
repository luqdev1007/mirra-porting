namespace Luqmus.MirraPorting.Core
{
    /// <summary>
    /// A file the scanner stopped reading as code partway through, because a comment, a verbatim
    /// string or a conditional never closed. Findings below that line are lost silently, so the
    /// report names the file and the line.
    /// </summary>
    internal sealed class TruncatedFile
    {
        internal TruncatedFile(string path, int line, string code)
        {
            Path = path ?? string.Empty;
            Line = line;
            Code = code ?? string.Empty;
        }

        /// <summary>Project relative path.</summary>
        internal string Path { get; }

        /// <summary>Line the file stopped being read as code from.</summary>
        internal int Line { get; }

        /// <summary>Diagnostic code, for example UnterminatedComment.</summary>
        internal string Code { get; }
    }
}
