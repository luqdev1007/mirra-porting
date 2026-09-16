using Luqmus.MirraPorting.Code;
using NUnit.Framework;
using static Luqmus.MirraPorting.Tests.Code.LexerAssert;

namespace Luqmus.MirraPorting.Tests.Code
{
    public class CSharpLexerDirectivesTests
    {
        [Test]
        public void Lex_ConditionalDirectivesAroundCode()
        {
            Expect(
                "#if UNITY_EDITOR\nTime\n#endif",
                T(TokenKind.Directive, "#if UNITY_EDITOR", 1, 1),
                T(TokenKind.Identifier, "Time", 2, 1),
                T(TokenKind.Directive, "#endif", 3, 1));
        }

        [Test]
        public void Lex_PragmaAndRegionAreDirectivesToo()
        {
            Expect(
                "#pragma warning disable 0649\n#region Fields\n#endregion",
                T(TokenKind.Directive, "#pragma warning disable 0649", 1, 1),
                T(TokenKind.Directive, "#region Fields", 2, 1),
                T(TokenKind.Directive, "#endregion", 3, 1));
        }

        [Test]
        public void Lex_IndentedDirectiveKeepsItsColumn()
        {
            Expect(
                "    #if X",
                T(TokenKind.Directive, "#if X", 1, 5));
        }

        [Test]
        public void Lex_TrailingCommentStaysInTheDirectiveText()
        {
            Expect(
                "#if UNITY_EDITOR // note",
                T(TokenKind.Directive, "#if UNITY_EDITOR // note", 1, 1));
        }

        [Test]
        public void Lex_TrailingWhitespaceIsNotPartOfTheDirective()
        {
            Expect(
                "#if X   \nTime",
                T(TokenKind.Directive, "#if X", 1, 1),
                T(TokenKind.Identifier, "Time", 2, 1));
        }

        [Test]
        public void Lex_HashInTheMiddleOfALineIsNotADirective()
        {
            Expect(
                "a # b",
                T(TokenKind.Identifier, "a", 1, 1),
                T(TokenKind.Punct, "#", 1, 3),
                T(TokenKind.Identifier, "b", 1, 5));
        }

        [Test]
        public void Lex_HashInsideAnInterpolationHoleIsNotADirective()
        {
            Expect(
                "$@\"{\n#if X\n}\"",
                T(TokenKind.String, "$@\"", 1, 1),
                T(TokenKind.Punct, "{", 1, 4),
                T(TokenKind.Punct, "#", 2, 1),
                T(TokenKind.Identifier, "if", 2, 2),
                T(TokenKind.Identifier, "X", 2, 5),
                T(TokenKind.Punct, "}", 3, 1),
                T(TokenKind.String, "\"", 3, 2));
        }
    }
}
