namespace Luqmus.MirraPorting.Code
{
    /// <summary>
    /// The directives that shape conditional regions. Everything else — #region, #pragma, #define,
    /// #undef, #line, #warning — is Other and does not affect them.
    /// </summary>
    internal enum DirectiveKind
    {
        Other = 0,
        If = 1,
        Elif = 2,
        Else = 3,
        EndIf = 4,
    }
}
