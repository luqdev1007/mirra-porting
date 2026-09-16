using System;
using System.Collections.Generic;

namespace Luqmus.MirraPorting.Code
{
    /// <summary>
    /// Evaluates the condition of #if and #elif under player build assumptions: UNITY_EDITOR and
    /// its platform variants are not defined, true and false are themselves, and every other
    /// symbol could go either way. A branch whose condition comes out False is code the player
    /// build never sees.
    ///
    /// The condition is lexed with <see cref="CSharpLexer"/>, which also drops a trailing comment.
    /// </summary>
    internal static class ConditionEvaluator
    {
        private const string EditorSymbolPrefix = "UNITY_EDITOR";

        /// <summary>
        /// Evaluates a condition. Returns false when it cannot be parsed, leaving the value
        /// Unknown, so that a condition nobody understands never marks code as editor only.
        /// </summary>
        internal static bool TryEvaluate(string condition, out ConditionValue value)
        {
            value = ConditionValue.Unknown;
            if (string.IsNullOrEmpty(condition))
            {
                return false;
            }

            IReadOnlyList<Token> tokens = CSharpLexer.Lex(condition).Tokens;
            if (tokens.Count == 0)
            {
                return false;
            }

            var parser = new Parser(tokens);
            ConditionValue parsed;
            if (!parser.TryParseOr(out parsed) || !parser.AtEnd)
            {
                return false;
            }

            value = parsed;
            return true;
        }

        /// <summary>
        /// The symbols a player build never has. The known set is UNITY_EDITOR, UNITY_EDITOR_WIN,
        /// UNITY_EDITOR_OSX, UNITY_EDITOR_LINUX and UNITY_EDITOR_64; the prefix covers whatever
        /// Unity adds next.
        /// </summary>
        private static ConditionValue ValueOfSymbol(string symbol)
        {
            if (string.Equals(symbol, "true", StringComparison.Ordinal))
            {
                return ConditionValue.True;
            }

            if (string.Equals(symbol, "false", StringComparison.Ordinal))
            {
                return ConditionValue.False;
            }

            if (symbol.StartsWith(EditorSymbolPrefix, StringComparison.Ordinal))
            {
                return ConditionValue.False;
            }

            return ConditionValue.Unknown;
        }

        /// <summary>
        /// Recursive descent over the operators C# allows in a condition:
        /// || then &amp;&amp; then == and != then ! then a symbol or a parenthesised condition.
        /// </summary>
        private sealed class Parser
        {
            private readonly IReadOnlyList<Token> _tokens;
            private int _index;

            internal Parser(IReadOnlyList<Token> tokens)
            {
                _tokens = tokens;
            }

            internal bool AtEnd
            {
                get { return _index >= _tokens.Count; }
            }

            internal bool TryParseOr(out ConditionValue value)
            {
                if (!TryParseAnd(out value))
                {
                    return false;
                }

                while (IsPunct("||"))
                {
                    _index++;
                    ConditionValue right;
                    if (!TryParseAnd(out right))
                    {
                        return false;
                    }

                    value = ConditionLogic.Or(value, right);
                }

                return true;
            }

            private bool TryParseAnd(out ConditionValue value)
            {
                if (!TryParseEquality(out value))
                {
                    return false;
                }

                while (IsPunct("&&"))
                {
                    _index++;
                    ConditionValue right;
                    if (!TryParseEquality(out right))
                    {
                        return false;
                    }

                    value = ConditionLogic.And(value, right);
                }

                return true;
            }

            private bool TryParseEquality(out ConditionValue value)
            {
                if (!TryParseUnary(out value))
                {
                    return false;
                }

                while (IsPunct("==") || IsPunct("!="))
                {
                    bool equals = IsPunct("==");
                    _index++;
                    ConditionValue right;
                    if (!TryParseUnary(out right))
                    {
                        return false;
                    }

                    value = equals
                        ? ConditionLogic.AreEqual(value, right)
                        : ConditionLogic.AreNotEqual(value, right);
                }

                return true;
            }

            private bool TryParseUnary(out ConditionValue value)
            {
                if (IsPunct("!"))
                {
                    _index++;
                    ConditionValue operand;
                    if (!TryParseUnary(out operand))
                    {
                        value = ConditionValue.Unknown;
                        return false;
                    }

                    value = ConditionLogic.Not(operand);
                    return true;
                }

                return TryParsePrimary(out value);
            }

            private bool TryParsePrimary(out ConditionValue value)
            {
                value = ConditionValue.Unknown;
                if (AtEnd)
                {
                    return false;
                }

                if (IsPunct("("))
                {
                    _index++;
                    if (!TryParseOr(out value))
                    {
                        return false;
                    }

                    if (!IsPunct(")"))
                    {
                        return false;
                    }

                    _index++;
                    return true;
                }

                Token token = _tokens[_index];
                if (token.Kind != TokenKind.Identifier)
                {
                    return false;
                }

                _index++;
                value = ValueOfSymbol(token.Text);
                return true;
            }

            private bool IsPunct(string text)
            {
                if (AtEnd)
                {
                    return false;
                }

                Token token = _tokens[_index];
                return token.Kind == TokenKind.Punct && string.Equals(token.Text, text, StringComparison.Ordinal);
            }
        }
    }
}
