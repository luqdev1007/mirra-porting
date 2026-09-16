namespace Luqmus.MirraPorting.Code
{
    /// <summary>
    /// What the lexical pass could not make sense of, tokens and conditional directives alike.
    /// Only a code and a position: the wording belongs to the layer that reports it, so that this
    /// folder stays free of user facing text.
    /// </summary>
    internal enum LexDiagnosticCode
    {
        UnterminatedString = 0,
        UnterminatedComment = 1,
        UnterminatedCharLiteral = 2,

        /// <summary>The lexer or the directive pass threw. Should never happen, reported instead of propagating.</summary>
        InternalError = 3,

        /// <summary>#endif, #elif or #else without a matching #if, or a second #else.</summary>
        UnbalancedDirective = 4,

        /// <summary>#if never closed before the end of the file.</summary>
        UnclosedConditional = 5,

        /// <summary>The condition of #if or #elif could not be parsed; it counts as unknown.</summary>
        InvalidConditionExpression = 6,
    }
}
