namespace Luqmus.MirraPorting.Code
{
    /// <summary>
    /// What the lexer could not make sense of. Only a code and a position: the wording belongs to
    /// the layer that turns a diagnostic into a finding, so that this folder stays free of user
    /// facing text.
    /// </summary>
    internal enum LexDiagnosticCode
    {
        UnterminatedString = 0,
        UnterminatedComment = 1,
        UnterminatedCharLiteral = 2,

        /// <summary>The lexer itself threw. Should never happen, reported instead of propagating.</summary>
        InternalError = 3,
    }
}
