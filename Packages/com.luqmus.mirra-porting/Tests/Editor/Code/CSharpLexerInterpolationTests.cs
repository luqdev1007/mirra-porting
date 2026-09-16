using Luqmus.MirraPorting.Code;
using NUnit.Framework;
using static Luqmus.MirraPorting.Tests.Code.LexerAssert;

namespace Luqmus.MirraPorting.Tests.Code
{
    public class CSharpLexerInterpolationTests
    {
        [Test]
        public void Lex_HoleWithAFormatSpecification()
        {
            Expect(
                "$\"scale {Time.timeScale:F2}\"",
                T(TokenKind.String, "$\"scale ", 1, 1),
                T(TokenKind.Punct, "{", 1, 9),
                T(TokenKind.Identifier, "Time", 1, 10),
                T(TokenKind.Punct, ".", 1, 14),
                T(TokenKind.Identifier, "timeScale", 1, 15),
                T(TokenKind.Punct, ":", 1, 24),
                T(TokenKind.String, "F2", 1, 25),
                T(TokenKind.Punct, "}", 1, 27),
                T(TokenKind.String, "\"", 1, 28));
        }

        [Test]
        public void Lex_DoubledBracesAreTextAndNeverBecomeCode()
        {
            Expect(
                "$\"{{Time.timeScale}}\"",
                T(TokenKind.String, "$\"{{Time.timeScale}}\"", 1, 1));
        }

        [Test]
        public void Lex_VerbatimInterpolationSpansLines()
        {
            Expect(
                "$@\"a\n{Time.timeScale}\"",
                T(TokenKind.String, "$@\"a\n", 1, 1),
                T(TokenKind.Punct, "{", 2, 1),
                T(TokenKind.Identifier, "Time", 2, 2),
                T(TokenKind.Punct, ".", 2, 6),
                T(TokenKind.Identifier, "timeScale", 2, 7),
                T(TokenKind.Punct, "}", 2, 16),
                T(TokenKind.String, "\"", 2, 17));
        }

        [Test]
        public void Lex_VerbatimInterpolationWithTheOtherPrefixOrder()
        {
            Expect(
                "@$\"a\n{Time.timeScale}\"",
                T(TokenKind.String, "@$\"a\n", 1, 1),
                T(TokenKind.Punct, "{", 2, 1),
                T(TokenKind.Identifier, "Time", 2, 2),
                T(TokenKind.Punct, ".", 2, 6),
                T(TokenKind.Identifier, "timeScale", 2, 7),
                T(TokenKind.Punct, "}", 2, 16),
                T(TokenKind.String, "\"", 2, 17));
        }

        [Test]
        public void Lex_ColonInsideParenthesesIsAnOperatorNotAFormat()
        {
            Expect(
                "$\"{(f ? Time.timeScale : 1f)}\"",
                T(TokenKind.String, "$\"", 1, 1),
                T(TokenKind.Punct, "{", 1, 3),
                T(TokenKind.Punct, "(", 1, 4),
                T(TokenKind.Identifier, "f", 1, 5),
                T(TokenKind.Punct, "?", 1, 7),
                T(TokenKind.Identifier, "Time", 1, 9),
                T(TokenKind.Punct, ".", 1, 13),
                T(TokenKind.Identifier, "timeScale", 1, 14),
                T(TokenKind.Punct, ":", 1, 24),
                T(TokenKind.Number, "1f", 1, 26),
                T(TokenKind.Punct, ")", 1, 28),
                T(TokenKind.Punct, "}", 1, 29),
                T(TokenKind.String, "\"", 1, 30));
        }

        [Test]
        public void Lex_StringInsideAHoleKeepsItsClosingBraceToItself()
        {
            Expect(
                "$\"{ \"a}\" + Time.timeScale }\"",
                T(TokenKind.String, "$\"", 1, 1),
                T(TokenKind.Punct, "{", 1, 3),
                T(TokenKind.String, "\"a}\"", 1, 5),
                T(TokenKind.Punct, "+", 1, 10),
                T(TokenKind.Identifier, "Time", 1, 12),
                T(TokenKind.Punct, ".", 1, 16),
                T(TokenKind.Identifier, "timeScale", 1, 17),
                T(TokenKind.Punct, "}", 1, 27),
                T(TokenKind.String, "\"", 1, 28));
        }

        [Test]
        public void Lex_BraceInsideACharLiteralDoesNotCloseTheHole()
        {
            Expect(
                "$\"{(c == '}' ? 1 : 2)}\"",
                T(TokenKind.String, "$\"", 1, 1),
                T(TokenKind.Punct, "{", 1, 3),
                T(TokenKind.Punct, "(", 1, 4),
                T(TokenKind.Identifier, "c", 1, 5),
                T(TokenKind.Punct, "==", 1, 7),
                T(TokenKind.Char, "'}'", 1, 10),
                T(TokenKind.Punct, "?", 1, 14),
                T(TokenKind.Number, "1", 1, 16),
                T(TokenKind.Punct, ":", 1, 18),
                T(TokenKind.Number, "2", 1, 20),
                T(TokenKind.Punct, ")", 1, 21),
                T(TokenKind.Punct, "}", 1, 22),
                T(TokenKind.String, "\"", 1, 23));
        }

        [Test]
        public void Lex_EscapedQuoteInsideInterpolatedText()
        {
            Expect(
                "$\"a\\\"b {x}\"",
                T(TokenKind.String, "$\"a\\\"b ", 1, 1),
                T(TokenKind.Punct, "{", 1, 8),
                T(TokenKind.Identifier, "x", 1, 9),
                T(TokenKind.Punct, "}", 1, 10),
                T(TokenKind.String, "\"", 1, 11));
        }

        [Test]
        public void Lex_EscapedBackslashEndsTheInterpolatedString()
        {
            Expect(
                "$\"a\\\\\" + x",
                T(TokenKind.String, "$\"a\\\\\"", 1, 1),
                T(TokenKind.Punct, "+", 1, 8),
                T(TokenKind.Identifier, "x", 1, 10));
        }

        [Test]
        public void Lex_NestedInterpolation()
        {
            Expect(
                "$\"{$\"{x}\"}\"",
                T(TokenKind.String, "$\"", 1, 1),
                T(TokenKind.Punct, "{", 1, 3),
                T(TokenKind.String, "$\"", 1, 4),
                T(TokenKind.Punct, "{", 1, 6),
                T(TokenKind.Identifier, "x", 1, 7),
                T(TokenKind.Punct, "}", 1, 8),
                T(TokenKind.String, "\"", 1, 9),
                T(TokenKind.Punct, "}", 1, 10),
                T(TokenKind.String, "\"", 1, 11));
        }

        [Test]
        public void Lex_AlignmentThenFormat()
        {
            Expect(
                "$\"{a,10:F2}\"",
                T(TokenKind.String, "$\"", 1, 1),
                T(TokenKind.Punct, "{", 1, 3),
                T(TokenKind.Identifier, "a", 1, 4),
                T(TokenKind.Punct, ",", 1, 5),
                T(TokenKind.Number, "10", 1, 6),
                T(TokenKind.Punct, ":", 1, 8),
                T(TokenKind.String, "F2", 1, 9),
                T(TokenKind.Punct, "}", 1, 11),
                T(TokenKind.String, "\"", 1, 12));
        }

        [Test]
        public void Lex_EmptyChunkBetweenTwoHolesIsNotEmitted()
        {
            Expect(
                "$\"{a}{b}\"",
                T(TokenKind.String, "$\"", 1, 1),
                T(TokenKind.Punct, "{", 1, 3),
                T(TokenKind.Identifier, "a", 1, 4),
                T(TokenKind.Punct, "}", 1, 5),
                T(TokenKind.Punct, "{", 1, 6),
                T(TokenKind.Identifier, "b", 1, 7),
                T(TokenKind.Punct, "}", 1, 8),
                T(TokenKind.String, "\"", 1, 9));
        }

        [Test]
        public void Lex_BracesInsideAHoleAreCountedBeforeTheHoleCloses()
        {
            Expect(
                "$\"{ new[]{1,2}.Length }\"",
                T(TokenKind.String, "$\"", 1, 1),
                T(TokenKind.Punct, "{", 1, 3),
                T(TokenKind.Identifier, "new", 1, 5),
                T(TokenKind.Punct, "[", 1, 8),
                T(TokenKind.Punct, "]", 1, 9),
                T(TokenKind.Punct, "{", 1, 10),
                T(TokenKind.Number, "1", 1, 11),
                T(TokenKind.Punct, ",", 1, 12),
                T(TokenKind.Number, "2", 1, 13),
                T(TokenKind.Punct, "}", 1, 14),
                T(TokenKind.Punct, ".", 1, 15),
                T(TokenKind.Identifier, "Length", 1, 16),
                T(TokenKind.Punct, "}", 1, 23),
                T(TokenKind.String, "\"", 1, 24));
        }

        [Test]
        public void Lex_LineBreakEndsANonVerbatimInterpolationAndTheCodeGoesOn()
        {
            LexResult result = Expect(
                "$\"abc\nint x;",
                T(TokenKind.String, "$\"abc", 1, 1),
                T(TokenKind.Identifier, "int", 2, 1),
                T(TokenKind.Identifier, "x", 2, 5),
                T(TokenKind.Punct, ";", 2, 6));

            Diagnostics(result, LexDiagnosticCode.UnterminatedString);
        }

        [Test]
        public void Lex_LineBreakUnwindsOnlyUpToTheNearestVerbatimFrame()
        {
            LexResult result = Expect(
                "$@\"outer { $\"inner\nx;",
                T(TokenKind.String, "$@\"outer ", 1, 1),
                T(TokenKind.Punct, "{", 1, 10),
                T(TokenKind.String, "$\"inner", 1, 12),
                T(TokenKind.Identifier, "x", 2, 1),
                T(TokenKind.Punct, ";", 2, 2));

            // The inner string died with the line; the verbatim one outlived it and was only
            // reported unterminated at the end of the file, where it had swallowed the rest.
            Diagnostics(
                result, LexDiagnosticCode.UnterminatedString, LexDiagnosticCode.UnterminatedVerbatimString);
        }

        [Test]
        public void Lex_HoleLeftOpenAtTheEndOfTheFileIsReported()
        {
            LexResult result = Expect(
                "$\"{",
                T(TokenKind.String, "$\"", 1, 1),
                T(TokenKind.Punct, "{", 1, 3));

            Diagnostics(result, LexDiagnosticCode.UnterminatedString);
        }
    }
}
