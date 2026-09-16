namespace Luqmus.MirraPorting.Code
{
    /// <summary>What a token is. Explicit values: tests compare token sequences by kind.</summary>
    internal enum TokenKind
    {
        Identifier = 0,
        Number = 1,
        String = 2,
        Char = 3,
        Punct = 4,
        Directive = 5,
    }
}
