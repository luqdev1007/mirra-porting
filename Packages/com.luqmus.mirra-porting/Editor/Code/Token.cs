namespace Luqmus.MirraPorting.Code
{
    /// <summary>
    /// One token. Start and Length point back into the source the lexer was given: the raw text of
    /// the token is always source.Substring(Start, Length), which lets tests check positions
    /// against the source instead of trusting the lexer's own bookkeeping.
    /// </summary>
    internal readonly struct Token
    {
        internal Token(TokenKind kind, string text, int line, int column, int start, int length)
        {
            Kind = kind;
            Text = text;
            Line = line;
            Column = column;
            Start = start;
            Length = length;
        }

        internal TokenKind Kind { get; }

        /// <summary>
        /// Token text. The same as the raw source text, except for a verbatim identifier, where
        /// the leading @ is dropped so that @class reads as class.
        /// </summary>
        internal string Text { get; }

        /// <summary>1-based line of the first character.</summary>
        internal int Line { get; }

        /// <summary>1-based column of the first character, counted in UTF-16 code units.</summary>
        internal int Column { get; }

        /// <summary>Offset of the first character in the source.</summary>
        internal int Start { get; }

        /// <summary>Length of the raw text in the source.</summary>
        internal int Length { get; }

        public override string ToString()
        {
            return Kind + " '" + Text + "' at " + Line + ":" + Column;
        }
    }
}
