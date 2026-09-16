using System;
using System.Collections.Generic;

namespace Luqmus.MirraPorting.Code
{
    /// <summary>
    /// Collects the names a file declares: types and the segments of its namespace names. A
    /// project that has its own Time or Cursor can shadow the Unity type of that name, which is
    /// why a match on such a name is only ever reported with low confidence.
    ///
    /// Names are case sensitive, like C# identifiers.
    /// </summary>
    internal static class DeclaredNameCollector
    {
        private static readonly string[] TypeKeywords = { "class", "struct", "enum", "interface" };

        internal static IReadOnlyList<string> Collect(IReadOnlyList<Token> tokens)
        {
            var names = new List<string>();
            if (tokens == null || tokens.Count == 0)
            {
                return names;
            }

            var seen = new HashSet<string>(StringComparer.Ordinal);

            for (int i = 0; i < tokens.Count; i++)
            {
                Token token = tokens[i];
                if (token.Kind != TokenKind.Identifier)
                {
                    continue;
                }

                if (string.Equals(token.Text, "namespace", StringComparison.Ordinal))
                {
                    i = CollectNamespaceParts(tokens, i + 1, names, seen);
                    continue;
                }

                if (IsTypeKeyword(token.Text) && IsDeclaration(tokens, i))
                {
                    Add(names, seen, tokens[i + 1].Text);
                }
            }

            return names;
        }

        /// <summary>
        /// A type keyword declares a type only when a name follows it. In a generic constraint the
        /// keyword follows a colon or a comma (where T : class, IFoo) or is followed by the next
        /// where (where T : struct where U : new()).
        /// </summary>
        private static bool IsDeclaration(IReadOnlyList<Token> tokens, int index)
        {
            if (index + 1 >= tokens.Count || tokens[index + 1].Kind != TokenKind.Identifier)
            {
                return false;
            }

            if (string.Equals(tokens[index + 1].Text, "where", StringComparison.Ordinal))
            {
                return false;
            }

            if (index > 0 && tokens[index - 1].Kind == TokenKind.Punct)
            {
                string previous = tokens[index - 1].Text;
                if (previous == ":" || previous == ",")
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>Adds every segment of namespace Game.Time and returns the last index it read.</summary>
        private static int CollectNamespaceParts(
            IReadOnlyList<Token> tokens, int index, List<string> names, HashSet<string> seen)
        {
            if (index >= tokens.Count || tokens[index].Kind != TokenKind.Identifier)
            {
                return index - 1;
            }

            Add(names, seen, tokens[index].Text);
            index++;

            while (index + 1 < tokens.Count &&
                   tokens[index].Kind == TokenKind.Punct &&
                   tokens[index].Text == "." &&
                   tokens[index + 1].Kind == TokenKind.Identifier)
            {
                Add(names, seen, tokens[index + 1].Text);
                index += 2;
            }

            return index - 1;
        }

        private static bool IsTypeKeyword(string text)
        {
            foreach (string keyword in TypeKeywords)
            {
                if (string.Equals(text, keyword, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static void Add(List<string> names, HashSet<string> seen, string name)
        {
            if (!string.IsNullOrEmpty(name) && seen.Add(name))
            {
                names.Add(name);
            }
        }
    }
}
