using System;
using System.Collections.Generic;
using Luqmus.MirraPorting.Code;

namespace Luqmus.MirraPorting.Checks
{
    /// <summary>
    /// Matches one place in the token stream against the forbidden API table:
    /// [global ::] [namespace .] Type . Member, or a bare member brought in by using static.
    /// </summary>
    internal static class ChainMatcher
    {
        private static readonly string[] AssignmentOperators =
        {
            "=", "+=", "-=", "*=", "/=", "%=", "&=", "|=", "^=", "<<=", ">>=", "??=",
        };

        /// <summary>
        /// Keywords after which an expression follows, so a bare member name there is a use and
        /// not a declaration.
        /// </summary>
        private static readonly string[] ExpressionKeywords =
        {
            "return", "case", "in", "is", "as", "else", "throw", "await", "yield", "when",
        };

        /// <summary>
        /// Tries to match a chain that starts at <paramref name="tokenIndex"/>. Only a token that
        /// really starts a chain is considered, so the middle of one is never matched again.
        /// </summary>
        internal static bool TryMatchAt(
            IReadOnlyList<Token> tokens, int tokenIndex, FileTypeIndex typeIndex, out ChainMatch match)
        {
            match = default(ChainMatch);

            if (tokens[tokenIndex].Kind != TokenKind.Identifier || !StartsAChain(tokens, tokenIndex))
            {
                return false;
            }

            if (IsInsideNameof(tokens, tokenIndex))
            {
                return false;
            }

            var identifiers = new List<int>();
            int afterChain = ReadChain(tokens, tokenIndex, identifiers);
            if (identifiers.Count == 0)
            {
                return false;
            }

            TypeNameResolution type;
            ForbiddenMember member;
            int memberIndex;

            if (!TryResolve(tokens, identifiers, typeIndex, out type, out member, out memberIndex))
            {
                return false;
            }

            // Everything after the member is a longer expression, Time.timeScale.ToString(); the
            // member itself decides how the chain is touched.
            int afterMember = memberIndex + 1 < identifiers.Count ? identifiers[memberIndex + 1] - 1 : afterChain;

            Token decider;
            AccessKind access = ResolveAccess(tokens, tokenIndex, identifiers[memberIndex], afterMember, out decider);

            if (member.RuleIdFor(access) == null)
            {
                return false;
            }

            match = new ChainMatch(
                tokens[tokenIndex], tokens[identifiers[memberIndex]], decider, member, access, type);
            return true;
        }

        /// <summary>
        /// A chain starts where a qualifier does not end. Anything behind a dot, a global
        /// qualifier or a null conditional access belongs to a chain already being read.
        /// </summary>
        private static bool StartsAChain(IReadOnlyList<Token> tokens, int tokenIndex)
        {
            if (tokenIndex == 0)
            {
                return true;
            }

            Token previous = tokens[tokenIndex - 1];
            if (previous.Kind != TokenKind.Punct)
            {
                return true;
            }

            return previous.Text != "." && previous.Text != "::" && previous.Text != "?.";
        }

        /// <summary>
        /// Reads [global ::] Identifier (. Identifier)* and collects the index of each identifier.
        /// Returns the index of the token right after the chain.
        /// </summary>
        private static int ReadChain(IReadOnlyList<Token> tokens, int tokenIndex, List<int> identifiers)
        {
            int index = tokenIndex;

            if (IsIdentifier(tokens, index, "global") && IsPunct(tokens, index + 1, "::"))
            {
                index += 2;
            }

            while (IsIdentifier(tokens, index))
            {
                identifiers.Add(index);
                index++;

                if (IsPunct(tokens, index, ".") && IsIdentifier(tokens, index + 1))
                {
                    index++;
                    continue;
                }

                break;
            }

            return index;
        }

        private static bool TryResolve(
            IReadOnlyList<Token> tokens,
            List<int> identifiers,
            FileTypeIndex typeIndex,
            out TypeNameResolution type,
            out ForbiddenMember member,
            out int memberIndex)
        {
            type = default(TypeNameResolution);
            member = null;
            memberIndex = -1;

            if (identifiers.Count == 1)
            {
                return TryResolveBareMember(tokens, identifiers[0], typeIndex, out type, out member, ref memberIndex);
            }

            if (typeIndex.TryResolveBareType(tokens[identifiers[0]].Text, out type) &&
                ForbiddenApiTable.TryFind(type.TypeName, tokens[identifiers[1]].Text, out member))
            {
                memberIndex = 1;
                return true;
            }

            // A qualified name: every identifier before the type has to spell its namespace.
            for (int split = 1; split <= identifiers.Count - 2; split++)
            {
                string qualifier = Join(tokens, identifiers, split);
                if (!typeIndex.TryResolveQualifiedType(qualifier, tokens[identifiers[split]].Text, out type))
                {
                    continue;
                }

                if (ForbiddenApiTable.TryFind(type.TypeName, tokens[identifiers[split + 1]].Text, out member))
                {
                    memberIndex = split + 1;
                    return true;
                }
            }

            member = null;
            return false;
        }

        /// <summary>
        /// A bare member name counts only where an expression can stand. After another identifier
        /// it is a declaration: float timeScale = 1f, void Quit(), a parameter.
        /// </summary>
        private static bool TryResolveBareMember(
            IReadOnlyList<Token> tokens,
            int index,
            FileTypeIndex typeIndex,
            out TypeNameResolution type,
            out ForbiddenMember member,
            ref int memberIndex)
        {
            type = default(TypeNameResolution);
            member = null;

            if (index > 0 && tokens[index - 1].Kind == TokenKind.Identifier &&
                !IsExpressionKeyword(tokens[index - 1].Text))
            {
                return false;
            }

            if (!typeIndex.TryResolveStaticMember(tokens[index].Text, out member, out type))
            {
                return false;
            }

            memberIndex = 0;
            return true;
        }

        private static AccessKind ResolveAccess(
            IReadOnlyList<Token> tokens, int chainStart, int memberTokenIndex, int afterMember, out Token decider)
        {
            if (afterMember < tokens.Count && tokens[afterMember].Kind == TokenKind.Punct)
            {
                string text = tokens[afterMember].Text;
                if (IsAssignment(text) || text == "++" || text == "--")
                {
                    decider = tokens[afterMember];
                    return AccessKind.Write;
                }

                if (text == "(")
                {
                    decider = tokens[afterMember];
                    return AccessKind.Call;
                }
            }

            if (chainStart > 0 && tokens[chainStart - 1].Kind == TokenKind.Punct &&
                (tokens[chainStart - 1].Text == "++" || tokens[chainStart - 1].Text == "--"))
            {
                decider = tokens[chainStart - 1];
                return AccessKind.Write;
            }

            decider = afterMember < tokens.Count ? tokens[afterMember] : tokens[memberTokenIndex];
            return AccessKind.Read;
        }

        /// <summary>nameof(Time.timeScale) names the member, it does not touch it.</summary>
        private static bool IsInsideNameof(IReadOnlyList<Token> tokens, int tokenIndex)
        {
            return tokenIndex >= 2 &&
                   IsPunct(tokens, tokenIndex - 1, "(") &&
                   IsIdentifier(tokens, tokenIndex - 2, "nameof");
        }

        private static string Join(IReadOnlyList<Token> tokens, List<int> identifiers, int count)
        {
            var text = new System.Text.StringBuilder();
            for (int i = 0; i < count; i++)
            {
                if (i > 0)
                {
                    text.Append('.');
                }

                text.Append(tokens[identifiers[i]].Text);
            }

            return text.ToString();
        }

        private static bool IsAssignment(string text)
        {
            foreach (string op in AssignmentOperators)
            {
                if (string.Equals(text, op, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsExpressionKeyword(string text)
        {
            foreach (string keyword in ExpressionKeywords)
            {
                if (string.Equals(text, keyword, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsIdentifier(IReadOnlyList<Token> tokens, int index)
        {
            return index >= 0 && index < tokens.Count && tokens[index].Kind == TokenKind.Identifier;
        }

        private static bool IsIdentifier(IReadOnlyList<Token> tokens, int index, string text)
        {
            return IsIdentifier(tokens, index) && string.Equals(tokens[index].Text, text, StringComparison.Ordinal);
        }

        private static bool IsPunct(IReadOnlyList<Token> tokens, int index, string text)
        {
            return index >= 0 &&
                   index < tokens.Count &&
                   tokens[index].Kind == TokenKind.Punct &&
                   string.Equals(tokens[index].Text, text, StringComparison.Ordinal);
        }
    }
}
