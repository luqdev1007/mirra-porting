using System.Collections.Generic;
using System.Linq;
using System.Text;
using Luqmus.MirraPorting.Code;
using NUnit.Framework;

namespace Luqmus.MirraPorting.Tests.Code
{
    /// <summary>What a token is expected to be. Start and Length are checked against the source instead.</summary>
    internal sealed class ExpectedToken
    {
        internal ExpectedToken(TokenKind kind, string text, int line, int column)
        {
            Kind = kind;
            Text = text;
            Line = line;
            Column = column;
        }

        internal TokenKind Kind { get; }

        internal string Text { get; }

        internal int Line { get; }

        internal int Column { get; }

        public override string ToString()
        {
            return Kind + " '" + Text + "' at " + Line + ":" + Column;
        }
    }

    /// <summary>
    /// Lexes and checks. Every test goes through here, so every test also verifies the invariants
    /// that tie tokens back to the source: the raw text at Start/Length, the position that offset
    /// really has, and that tokens run forward without overlapping.
    /// </summary>
    internal static class LexerAssert
    {
        internal static ExpectedToken T(TokenKind kind, string text, int line, int column)
        {
            return new ExpectedToken(kind, text, line, column);
        }

        /// <summary>Lexes, checks the invariants and the whole token sequence.</summary>
        internal static LexResult Expect(string source, params ExpectedToken[] expected)
        {
            LexResult result = Check(source);

            string actual = Dump(result.Tokens);
            Assert.AreEqual(expected.Length, result.Tokens.Count, "Token count. Actual tokens:\n" + actual);

            for (int i = 0; i < expected.Length; i++)
            {
                Token token = result.Tokens[i];
                ExpectedToken want = expected[i];
                string where = "Token " + i + ". Actual tokens:\n" + actual;

                Assert.AreEqual(want.Kind, token.Kind, where);
                Assert.AreEqual(want.Text, token.Text, where);
                Assert.AreEqual(want.Line, token.Line, where + "\nLine of token " + i);
                Assert.AreEqual(want.Column, token.Column, where + "\nColumn of token " + i);
            }

            return result;
        }

        /// <summary>Lexes and checks the invariants only, for tests about diagnostics.</summary>
        internal static LexResult Check(string source)
        {
            LexResult result = CSharpLexer.Lex(source);
            CheckInvariants(source, result);
            return result;
        }

        internal static void Diagnostics(LexResult result, params LexDiagnosticCode[] codes)
        {
            CollectionAssert.AreEqual(
                codes,
                result.Diagnostics.Select(d => d.Code).ToList(),
                "Diagnostics: " + string.Join(", ", result.Diagnostics.Select(d => d.ToString()).ToArray()));
        }

        internal static string Dump(IReadOnlyList<Token> tokens)
        {
            var text = new StringBuilder();
            for (int i = 0; i < tokens.Count; i++)
            {
                text.AppendLine("  [" + i + "] " + tokens[i]);
            }

            return text.ToString();
        }

        private static void CheckInvariants(string source, LexResult result)
        {
            int previousEnd = 0;
            for (int i = 0; i < result.Tokens.Count; i++)
            {
                Token token = result.Tokens[i];
                string where = "Token " + i + " " + token;

                Assert.Greater(token.Length, 0, where + ": empty tokens are never emitted");
                Assert.GreaterOrEqual(token.Start, previousEnd, where + ": tokens must not overlap or go backwards");
                Assert.LessOrEqual(token.Start + token.Length, source.Length, where + ": token runs past the source");

                string raw = source.Substring(token.Start, token.Length);
                if (token.Kind == TokenKind.Identifier && raw.Length > 0 && raw[0] == '@')
                {
                    Assert.AreEqual("@" + token.Text, raw, where + ": verbatim identifier keeps its @ in the source");
                }
                else
                {
                    Assert.AreEqual(raw, token.Text, where + ": text must be the raw source at Start/Length");
                }

                int line;
                int column;
                PositionOf(source, token.Start, out line, out column);
                Assert.AreEqual(line, token.Line, where + ": line does not match Start " + token.Start);
                Assert.AreEqual(column, token.Column, where + ": column does not match Start " + token.Start);

                previousEnd = token.Start + token.Length;
            }
        }

        /// <summary>
        /// Line and column of an offset, counted independently of the lexer. A leading byte order
        /// mark is not part of the first line.
        /// </summary>
        private static void PositionOf(string source, int offset, out int line, out int column)
        {
            int index = source.Length > 0 && source[0] == '﻿' ? 1 : 0;
            int lineStart = index;
            line = 1;

            while (index < offset)
            {
                char c = source[index];
                if (c == '\r')
                {
                    index++;
                    if (index < source.Length && source[index] == '\n')
                    {
                        index++;
                    }

                    line++;
                    lineStart = index;
                    continue;
                }

                if (c == '\n')
                {
                    index++;
                    line++;
                    lineStart = index;
                    continue;
                }

                index++;
            }

            column = offset - lineStart + 1;
        }
    }
}
