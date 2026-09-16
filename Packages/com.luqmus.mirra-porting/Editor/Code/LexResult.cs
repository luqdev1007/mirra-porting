using System.Collections.Generic;

namespace Luqmus.MirraPorting.Code
{
    /// <summary>
    /// Everything the lexer produced for one file: the tokens, what it could not make sense of,
    /// and the line boundaries it counted by, so that whoever builds a snippet splits lines exactly
    /// the way the line numbers were counted.
    /// </summary>
    internal sealed class LexResult
    {
        private readonly string _source;
        private readonly IReadOnlyList<int> _lineStarts;

        internal LexResult(
            string source,
            IReadOnlyList<Token> tokens,
            IReadOnlyList<LexDiagnostic> diagnostics,
            IReadOnlyList<int> lineStarts)
        {
            _source = source;
            _lineStarts = lineStarts;
            Tokens = tokens;
            Diagnostics = diagnostics;
        }

        internal IReadOnlyList<Token> Tokens { get; }

        internal IReadOnlyList<LexDiagnostic> Diagnostics { get; }

        internal bool HasDiagnostics
        {
            get { return Diagnostics.Count > 0; }
        }

        /// <summary>Number of lines, counting an empty source as one line.</summary>
        internal int LineCount
        {
            get { return _lineStarts.Count; }
        }

        /// <summary>
        /// Text of a 1-based line without its line break, or an empty string when the line is out
        /// of range.
        /// </summary>
        internal string GetLineText(int line)
        {
            if (line < 1 || line > _lineStarts.Count)
            {
                return string.Empty;
            }

            int start = _lineStarts[line - 1];
            int end = line < _lineStarts.Count ? _lineStarts[line] : _source.Length;
            while (end > start && (_source[end - 1] == '\n' || _source[end - 1] == '\r'))
            {
                end--;
            }

            return _source.Substring(start, end - start);
        }
    }
}
