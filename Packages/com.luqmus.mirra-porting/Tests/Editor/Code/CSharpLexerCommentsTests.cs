using Luqmus.MirraPorting.Code;
using NUnit.Framework;
using static Luqmus.MirraPorting.Tests.Code.LexerAssert;

namespace Luqmus.MirraPorting.Tests.Code
{
    public class CSharpLexerCommentsTests
    {
        [Test]
        public void Lex_LineCommentProducesNoTokens()
        {
            LexResult result = Expect("// Time.timeScale = 0;");

            Assert.IsFalse(result.HasDiagnostics);
        }

        [Test]
        public void Lex_BlockCommentKeepsTheLineNumbersAfterIt()
        {
            Expect(
                "/* строка1\n Time.timeScale = 0; */ int a;",
                T(TokenKind.Identifier, "int", 2, 25),
                T(TokenKind.Identifier, "a", 2, 29),
                T(TokenKind.Punct, ";", 2, 30));
        }

        [Test]
        public void Lex_DoubleSlashInsideAStringDoesNotStartAComment()
        {
            Expect(
                "var u = \"https://x.com\"; Time.timeScale = 0;",
                T(TokenKind.Identifier, "var", 1, 1),
                T(TokenKind.Identifier, "u", 1, 5),
                T(TokenKind.Punct, "=", 1, 7),
                T(TokenKind.String, "\"https://x.com\"", 1, 9),
                T(TokenKind.Punct, ";", 1, 24),
                T(TokenKind.Identifier, "Time", 1, 26),
                T(TokenKind.Punct, ".", 1, 30),
                T(TokenKind.Identifier, "timeScale", 1, 31),
                T(TokenKind.Punct, "=", 1, 41),
                T(TokenKind.Number, "0", 1, 43),
                T(TokenKind.Punct, ";", 1, 44));
        }

        [Test]
        public void Lex_HashInsideABlockCommentIsNotADirective()
        {
            Expect(
                "/*\n#if X\n*/\nTime",
                T(TokenKind.Identifier, "Time", 4, 1));
        }

        [Test]
        public void Lex_UnterminatedBlockCommentIsReportedAndSwallowsTheRest()
        {
            LexResult result = Expect(
                "int a; /* tail",
                T(TokenKind.Identifier, "int", 1, 1),
                T(TokenKind.Identifier, "a", 1, 5),
                T(TokenKind.Punct, ";", 1, 6));

            Diagnostics(result, LexDiagnosticCode.UnterminatedComment);
            Assert.AreEqual(1, result.Diagnostics[0].Line);
            Assert.AreEqual(8, result.Diagnostics[0].Column);
        }
    }
}
