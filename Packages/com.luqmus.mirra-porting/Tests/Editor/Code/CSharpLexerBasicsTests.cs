using Luqmus.MirraPorting.Code;
using NUnit.Framework;
using static Luqmus.MirraPorting.Tests.Code.LexerAssert;

namespace Luqmus.MirraPorting.Tests.Code
{
    public class CSharpLexerBasicsTests
    {
        [Test]
        public void Lex_IdentifiersNumbersAndPunctuation()
        {
            Expect(
                "var x = 1;",
                T(TokenKind.Identifier, "var", 1, 1),
                T(TokenKind.Identifier, "x", 1, 5),
                T(TokenKind.Punct, "=", 1, 7),
                T(TokenKind.Number, "1", 1, 9),
                T(TokenKind.Punct, ";", 1, 10));
        }

        [Test]
        public void Lex_CyrillicIdentifiers()
        {
            Expect(
                "var имя = 1; Время.x",
                T(TokenKind.Identifier, "var", 1, 1),
                T(TokenKind.Identifier, "имя", 1, 5),
                T(TokenKind.Punct, "=", 1, 9),
                T(TokenKind.Number, "1", 1, 11),
                T(TokenKind.Punct, ";", 1, 12),
                T(TokenKind.Identifier, "Время", 1, 14),
                T(TokenKind.Punct, ".", 1, 19),
                T(TokenKind.Identifier, "x", 1, 20));
        }

        [TestCase("<<=")]
        [TestCase(">>=")]
        [TestCase("??=")]
        [TestCase("==")]
        [TestCase("!=")]
        [TestCase("<=")]
        [TestCase(">=")]
        [TestCase("=>")]
        [TestCase("+=")]
        [TestCase("-=")]
        [TestCase("*=")]
        [TestCase("/=")]
        [TestCase("%=")]
        [TestCase("&=")]
        [TestCase("|=")]
        [TestCase("^=")]
        [TestCase("??")]
        [TestCase("?.")]
        [TestCase("++")]
        [TestCase("--")]
        [TestCase("&&")]
        [TestCase("||")]
        [TestCase("::")]
        public void Lex_MultiCharacterOperatorIsOneToken(string op)
        {
            Expect(
                "a " + op + " b",
                T(TokenKind.Identifier, "a", 1, 1),
                T(TokenKind.Punct, op, 1, 3),
                T(TokenKind.Identifier, "b", 1, 4 + op.Length));
        }

        [Test]
        public void Lex_QuestionDotIsNotTakenFromAConditionalOverANumber()
        {
            Expect(
                "x ? .5f : 1f",
                T(TokenKind.Identifier, "x", 1, 1),
                T(TokenKind.Punct, "?", 1, 3),
                T(TokenKind.Number, ".5f", 1, 5),
                T(TokenKind.Punct, ":", 1, 9),
                T(TokenKind.Number, "1f", 1, 11));
        }

        [Test]
        public void Lex_NullConditionalAccess()
        {
            Expect(
                "x?.y",
                T(TokenKind.Identifier, "x", 1, 1),
                T(TokenKind.Punct, "?.", 1, 2),
                T(TokenKind.Identifier, "y", 1, 4));
        }

        [Test]
        public void Lex_NumberLiterals()
        {
            Expect(
                "0x1F 1_000 1e-5 1.5f .5f 1",
                T(TokenKind.Number, "0x1F", 1, 1),
                T(TokenKind.Number, "1_000", 1, 6),
                T(TokenKind.Number, "1e-5", 1, 12),
                T(TokenKind.Number, "1.5f", 1, 17),
                T(TokenKind.Number, ".5f", 1, 22),
                T(TokenKind.Number, "1", 1, 26));
        }

        [Test]
        public void Lex_MemberAccessOnANumberDoesNotSwallowTheDot()
        {
            Expect(
                "1.ToString()",
                T(TokenKind.Number, "1", 1, 1),
                T(TokenKind.Punct, ".", 1, 2),
                T(TokenKind.Identifier, "ToString", 1, 3),
                T(TokenKind.Punct, "(", 1, 11),
                T(TokenKind.Punct, ")", 1, 12));
        }

        [Test]
        public void Lex_VerbatimIdentifierDropsTheAtSign()
        {
            Expect(
                "var @class = 1;",
                T(TokenKind.Identifier, "var", 1, 1),
                T(TokenKind.Identifier, "class", 1, 5),
                T(TokenKind.Punct, "=", 1, 12),
                T(TokenKind.Number, "1", 1, 14),
                T(TokenKind.Punct, ";", 1, 15));
        }

        [Test]
        public void Lex_ChainBrokenOverThreeLinesKeepsEachPosition()
        {
            Expect(
                "Time\n    .timeScale\n    = 0f;",
                T(TokenKind.Identifier, "Time", 1, 1),
                T(TokenKind.Punct, ".", 2, 5),
                T(TokenKind.Identifier, "timeScale", 2, 6),
                T(TokenKind.Punct, "=", 3, 5),
                T(TokenKind.Number, "0f", 3, 7),
                T(TokenKind.Punct, ";", 3, 9));
        }

        [Test]
        public void Lex_GlobalAliasQualifier()
        {
            Expect(
                "global::UnityEngine.Time.timeScale = 1;",
                T(TokenKind.Identifier, "global", 1, 1),
                T(TokenKind.Punct, "::", 1, 7),
                T(TokenKind.Identifier, "UnityEngine", 1, 9),
                T(TokenKind.Punct, ".", 1, 20),
                T(TokenKind.Identifier, "Time", 1, 21),
                T(TokenKind.Punct, ".", 1, 25),
                T(TokenKind.Identifier, "timeScale", 1, 26),
                T(TokenKind.Punct, "=", 1, 36),
                T(TokenKind.Number, "1", 1, 38),
                T(TokenKind.Punct, ";", 1, 39));
        }

        [Test]
        public void Lex_NameofIsJustTokens()
        {
            Expect(
                "nameof(Time.timeScale)",
                T(TokenKind.Identifier, "nameof", 1, 1),
                T(TokenKind.Punct, "(", 1, 7),
                T(TokenKind.Identifier, "Time", 1, 8),
                T(TokenKind.Punct, ".", 1, 12),
                T(TokenKind.Identifier, "timeScale", 1, 13),
                T(TokenKind.Punct, ")", 1, 22));
        }

        [Test]
        public void Lex_CarriageReturnLineFeedIsOneLineBreak()
        {
            Expect(
                "a\r\nb",
                T(TokenKind.Identifier, "a", 1, 1),
                T(TokenKind.Identifier, "b", 2, 1));
        }

        [Test]
        public void Lex_LoneCarriageReturnIsALineBreak()
        {
            Expect(
                "a\rb",
                T(TokenKind.Identifier, "a", 1, 1),
                T(TokenKind.Identifier, "b", 2, 1));
        }

        [Test]
        public void Lex_ByteOrderMarkAndCyrillicCommentsKeepPositionsRight()
        {
            Expect(
                "﻿// комментарий\nTime.timeScale",
                T(TokenKind.Identifier, "Time", 2, 1),
                T(TokenKind.Punct, ".", 2, 5),
                T(TokenKind.Identifier, "timeScale", 2, 6));
        }

        [Test]
        public void Lex_TabCountsAsOneColumn()
        {
            Expect(
                "\tTime",
                T(TokenKind.Identifier, "Time", 1, 2));
        }

        [Test]
        public void Lex_EmptyAndNullSourceGiveNothing()
        {
            LexResult empty = Check(string.Empty);
            Assert.AreEqual(0, empty.Tokens.Count);
            Assert.IsFalse(empty.HasDiagnostics);
            Assert.AreEqual(1, empty.LineCount);

            LexResult none = CSharpLexer.Lex(null);
            Assert.AreEqual(0, none.Tokens.Count);
            Assert.IsFalse(none.HasDiagnostics);
        }

        [Test]
        public void GetLineText_ReturnsLinesWithoutTheirLineBreaks()
        {
            LexResult result = Check("a\nbb\r\nccc");

            Assert.AreEqual(3, result.LineCount);
            Assert.AreEqual("a", result.GetLineText(1));
            Assert.AreEqual("bb", result.GetLineText(2));
            Assert.AreEqual("ccc", result.GetLineText(3));
            Assert.AreEqual(string.Empty, result.GetLineText(0));
            Assert.AreEqual(string.Empty, result.GetLineText(4));
        }
    }
}
