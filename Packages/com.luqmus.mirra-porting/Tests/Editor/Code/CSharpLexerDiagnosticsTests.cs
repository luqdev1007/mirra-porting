using Luqmus.MirraPorting.Code;
using NUnit.Framework;
using static Luqmus.MirraPorting.Tests.Code.LexerAssert;

namespace Luqmus.MirraPorting.Tests.Code
{
    /// <summary>
    /// The lexer reads inactive #if branches as well, and those hold prose, half written code and
    /// deliberately broken snippets. None of it may hide the rest of the file.
    /// </summary>
    public class CSharpLexerDiagnosticsTests
    {
        [Test]
        public void Lex_BrokenStringInAnInactiveBranchDoesNotHideTheCodeBelow()
        {
            LexResult result = Expect(
                "#if false\n// what's\nDebug.Log(\"don't);\n#endif\nTime",
                T(TokenKind.Directive, "#if false", 1, 1),
                T(TokenKind.Identifier, "Debug", 3, 1),
                T(TokenKind.Punct, ".", 3, 6),
                T(TokenKind.Identifier, "Log", 3, 7),
                T(TokenKind.Punct, "(", 3, 10),
                T(TokenKind.String, "\"don't);", 3, 11),
                T(TokenKind.Directive, "#endif", 4, 1),
                T(TokenKind.Identifier, "Time", 5, 1));

            Diagnostics(result, LexDiagnosticCode.UnterminatedString);
        }

        [Test]
        public void Lex_ApostropheInAnInactiveBranchDoesNotHideTheCodeBelow()
        {
            LexResult result = Expect(
                "#if false\ndon't\n#endif\nx",
                T(TokenKind.Directive, "#if false", 1, 1),
                T(TokenKind.Identifier, "don", 2, 1),
                T(TokenKind.Punct, "'", 2, 4),
                T(TokenKind.Identifier, "t", 2, 5),
                T(TokenKind.Directive, "#endif", 3, 1),
                T(TokenKind.Identifier, "x", 4, 1));

            Diagnostics(result, LexDiagnosticCode.UnterminatedCharLiteral);
            Assert.AreEqual(2, result.Diagnostics[0].Line);
            Assert.AreEqual(4, result.Diagnostics[0].Column);
        }

        [TestCase("$\"{")]
        [TestCase("@\"")]
        [TestCase("'")]
        [TestCase("/*")]
        [TestCase("$\"")]
        [TestCase("$@\"{ $\"")]
        [TestCase("\"\\")]
        [TestCase("@")]
        [TestCase("#")]
        [TestCase("{{}}")]
        [TestCase("$\"{a:")]
        public void Lex_NeverThrowsOnBrokenInput(string source)
        {
            LexResult result = Check(source);

            Assert.IsNotNull(result);
            Assert.IsNotNull(result.Tokens);
            Assert.IsNotNull(result.Diagnostics);
        }
    }
}
