using Luqmus.MirraPorting.Code;
using NUnit.Framework;
using static Luqmus.MirraPorting.Tests.Code.LexerAssert;

namespace Luqmus.MirraPorting.Tests.Code
{
    public class CSharpLexerStringsTests
    {
        [Test]
        public void Lex_CodeInsideAStringIsJustAString()
        {
            Expect(
                "var s = \"Time.timeScale = 0\";",
                T(TokenKind.Identifier, "var", 1, 1),
                T(TokenKind.Identifier, "s", 1, 5),
                T(TokenKind.Punct, "=", 1, 7),
                T(TokenKind.String, "\"Time.timeScale = 0\"", 1, 9),
                T(TokenKind.Punct, ";", 1, 29));
        }

        [Test]
        public void Lex_VerbatimStringWithDoubledQuotes()
        {
            // var p = @"C:\dir\""x"""; Time.timeScale = 0;
            Expect(
                "var p = @\"C:\\dir\\\"\"x\"\"\"; Time.timeScale = 0;",
                T(TokenKind.Identifier, "var", 1, 1),
                T(TokenKind.Identifier, "p", 1, 5),
                T(TokenKind.Punct, "=", 1, 7),
                T(TokenKind.String, "@\"C:\\dir\\\"\"x\"\"\"", 1, 9),
                T(TokenKind.Punct, ";", 1, 24),
                T(TokenKind.Identifier, "Time", 1, 26),
                T(TokenKind.Punct, ".", 1, 30),
                T(TokenKind.Identifier, "timeScale", 1, 31),
                T(TokenKind.Punct, "=", 1, 41),
                T(TokenKind.Number, "0", 1, 43),
                T(TokenKind.Punct, ";", 1, 44));
        }

        [Test]
        public void Lex_VerbatimStringSpansLinesAndHidesADirective()
        {
            Expect(
                "@\"a\n#if X\n\";\nTime",
                T(TokenKind.String, "@\"a\n#if X\n\"", 1, 1),
                T(TokenKind.Punct, ";", 3, 2),
                T(TokenKind.Identifier, "Time", 4, 1));
        }

        [Test]
        public void Lex_QuoteInsideACharLiteral()
        {
            Expect(
                "char c = '\"'; Time.timeScale = 0;",
                T(TokenKind.Identifier, "char", 1, 1),
                T(TokenKind.Identifier, "c", 1, 6),
                T(TokenKind.Punct, "=", 1, 8),
                T(TokenKind.Char, "'\"'", 1, 10),
                T(TokenKind.Punct, ";", 1, 13),
                T(TokenKind.Identifier, "Time", 1, 15),
                T(TokenKind.Punct, ".", 1, 19),
                T(TokenKind.Identifier, "timeScale", 1, 20),
                T(TokenKind.Punct, "=", 1, 30),
                T(TokenKind.Number, "0", 1, 32),
                T(TokenKind.Punct, ";", 1, 33));
        }

        [Test]
        public void Lex_EscapedQuoteAndBackslashCharLiterals()
        {
            Expect(
                "char a = '\\''; char b = '\\\\';",
                T(TokenKind.Identifier, "char", 1, 1),
                T(TokenKind.Identifier, "a", 1, 6),
                T(TokenKind.Punct, "=", 1, 8),
                T(TokenKind.Char, "'\\''", 1, 10),
                T(TokenKind.Punct, ";", 1, 14),
                T(TokenKind.Identifier, "char", 1, 16),
                T(TokenKind.Identifier, "b", 1, 21),
                T(TokenKind.Punct, "=", 1, 23),
                T(TokenKind.Char, "'\\\\'", 1, 25),
                T(TokenKind.Punct, ";", 1, 29));
        }

        [Test]
        public void Lex_UnterminatedStringStopsAtTheLineAndTheRestStaysCode()
        {
            LexResult result = Expect(
                "\"abc\nint a;",
                T(TokenKind.String, "\"abc", 1, 1),
                T(TokenKind.Identifier, "int", 2, 1),
                T(TokenKind.Identifier, "a", 2, 5),
                T(TokenKind.Punct, ";", 2, 6));

            Diagnostics(result, LexDiagnosticCode.UnterminatedString);
        }

        [Test]
        public void Lex_BackslashAtTheEndOfALineDoesNotSwallowTheLineBreak()
        {
            LexResult result = Expect(
                "\"a\\\nb;",
                T(TokenKind.String, "\"a\\", 1, 1),
                T(TokenKind.Identifier, "b", 2, 1),
                T(TokenKind.Punct, ";", 2, 2));

            Diagnostics(result, LexDiagnosticCode.UnterminatedString);
        }

        [Test]
        public void Lex_ApostropheInProseIsReportedAndDoesNotEatTheLine()
        {
            LexResult result = Expect(
                "don't",
                T(TokenKind.Identifier, "don", 1, 1),
                T(TokenKind.Punct, "'", 1, 4),
                T(TokenKind.Identifier, "t", 1, 5));

            Diagnostics(result, LexDiagnosticCode.UnterminatedCharLiteral);
        }

        [Test]
        public void Lex_UnterminatedVerbatimStringRunsToTheEndOfTheFile()
        {
            LexResult result = Expect(
                "@\"abc\nstill inside",
                T(TokenKind.String, "@\"abc\nstill inside", 1, 1));

            Diagnostics(result, LexDiagnosticCode.UnterminatedString);
        }
    }
}
