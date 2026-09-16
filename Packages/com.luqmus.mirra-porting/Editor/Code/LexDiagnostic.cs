namespace Luqmus.MirraPorting.Code
{
    /// <summary>Where the lexer ran into something it could not make sense of.</summary>
    internal sealed class LexDiagnostic
    {
        internal LexDiagnostic(LexDiagnosticCode code, int line, int column)
        {
            Code = code;
            Line = line;
            Column = column;
        }

        internal LexDiagnosticCode Code { get; }

        /// <summary>1-based line where the offending construct starts.</summary>
        internal int Line { get; }

        /// <summary>1-based column where the offending construct starts.</summary>
        internal int Column { get; }

        public override string ToString()
        {
            return Code + " at " + Line + ":" + Column;
        }
    }
}
