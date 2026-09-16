namespace Luqmus.MirraPorting.Code
{
    /// <summary>
    /// A directive token split into its keyword and the rest of the line. C# allows whitespace
    /// between the hash and the keyword, so "#  if X" is the same directive as "#if X".
    /// </summary>
    internal readonly struct DirectiveLine
    {
        private DirectiveLine(DirectiveKind kind, string argument)
        {
            Kind = kind;
            Argument = argument;
        }

        internal DirectiveKind Kind { get; }

        /// <summary>
        /// Raw text after the keyword. For #if and #elif it is the condition, trailing comment and
        /// all: the condition is lexed later, and the lexer drops the comment itself.
        /// </summary>
        internal string Argument { get; }

        internal static DirectiveLine Parse(Token token)
        {
            string text = token.Text;
            if (string.IsNullOrEmpty(text) || text[0] != '#')
            {
                return new DirectiveLine(DirectiveKind.Other, string.Empty);
            }

            int index = 1;
            index = SkipWhitespace(text, index);

            int keywordStart = index;
            while (index < text.Length && char.IsLetter(text[index]))
            {
                index++;
            }

            string keyword = text.Substring(keywordStart, index - keywordStart);
            string argument = text.Substring(SkipWhitespace(text, index));

            return new DirectiveLine(KindOf(keyword), argument);
        }

        private static DirectiveKind KindOf(string keyword)
        {
            switch (keyword)
            {
                case "if":
                    return DirectiveKind.If;
                case "elif":
                    return DirectiveKind.Elif;
                case "else":
                    return DirectiveKind.Else;
                case "endif":
                    return DirectiveKind.EndIf;
                default:
                    return DirectiveKind.Other;
            }
        }

        private static int SkipWhitespace(string text, int index)
        {
            while (index < text.Length && char.IsWhiteSpace(text[index]))
            {
                index++;
            }

            return index;
        }
    }
}
