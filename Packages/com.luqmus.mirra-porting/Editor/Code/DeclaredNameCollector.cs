using System;
using System.Collections.Generic;

namespace Luqmus.MirraPorting.Code
{
    /// <summary>
    /// Collects the names a file declares: types and the segments of its namespace names, each
    /// with the namespace it sits in. A project that has its own Time or Cursor can shadow the
    /// Unity type of that name, but only where that declaration is actually visible.
    ///
    /// Names are case sensitive, like C# identifiers.
    /// </summary>
    internal static class DeclaredNameCollector
    {
        private static readonly string[] TypeKeywords = { "class", "struct", "enum", "interface" };

        internal static DeclaredNames Collect(IReadOnlyList<Token> tokens)
        {
            if (tokens == null || tokens.Count == 0)
            {
                return DeclaredNames.Empty;
            }

            var names = new List<DeclaredName>();
            var namespaces = new List<string>();
            var seenNames = new HashSet<string>(StringComparer.Ordinal);
            var seenNamespaces = new HashSet<string>(StringComparer.Ordinal);
            var braces = new List<NamespaceFrame>();

            string current = string.Empty;
            string pending = null;

            for (int i = 0; i < tokens.Count; i++)
            {
                Token token = tokens[i];

                if (token.Kind == TokenKind.Punct)
                {
                    if (token.Text == "{")
                    {
                        braces.Add(new NamespaceFrame(pending != null, current));
                        if (pending != null)
                        {
                            current = pending;
                            pending = null;
                        }
                    }
                    else if (token.Text == "}")
                    {
                        if (braces.Count > 0)
                        {
                            NamespaceFrame frame = braces[braces.Count - 1];
                            braces.RemoveAt(braces.Count - 1);
                            if (frame.ChangedNamespace)
                            {
                                current = frame.Previous;
                            }
                        }
                    }
                    else if (token.Text == ";" && pending != null)
                    {
                        // File scoped namespace: it covers the rest of the file, no brace follows.
                        current = pending;
                        pending = null;
                    }

                    continue;
                }

                if (token.Kind != TokenKind.Identifier)
                {
                    continue;
                }

                if (string.Equals(token.Text, "namespace", StringComparison.Ordinal))
                {
                    string declared;
                    i = ReadNamespaceName(tokens, i + 1, current, out declared);
                    if (declared != null)
                    {
                        pending = declared;
                        AddNamespace(namespaces, seenNamespaces, declared);
                        AddSegments(names, seenNames, declared);
                    }

                    continue;
                }

                if (IsTypeKeyword(token.Text) && IsDeclaration(tokens, i))
                {
                    // A type nested in another type keeps the namespace of the outer one: only a
                    // namespace declaration changes it.
                    Add(names, seenNames, tokens[i + 1].Text, current);
                }
            }

            return new DeclaredNames(names, namespaces);
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

        /// <summary>
        /// Reads the dotted name after the namespace keyword and returns the index of its last
        /// token. The result is the full name, with the enclosing namespace in front.
        /// </summary>
        private static int ReadNamespaceName(
            IReadOnlyList<Token> tokens, int index, string enclosing, out string declared)
        {
            declared = null;
            if (index >= tokens.Count || tokens[index].Kind != TokenKind.Identifier)
            {
                return index - 1;
            }

            string name = tokens[index].Text;
            index++;

            while (index + 1 < tokens.Count &&
                   tokens[index].Kind == TokenKind.Punct &&
                   tokens[index].Text == "." &&
                   tokens[index + 1].Kind == TokenKind.Identifier)
            {
                name = name + "." + tokens[index + 1].Text;
                index += 2;
            }

            declared = enclosing.Length == 0 ? name : enclosing + "." + name;
            return index - 1;
        }

        /// <summary>
        /// Every segment of a namespace name is a name of its own: Game.Time declares Time in
        /// Game, and Game in the global namespace.
        /// </summary>
        private static void AddSegments(List<DeclaredName> names, HashSet<string> seen, string fullName)
        {
            int start = 0;
            while (start <= fullName.Length)
            {
                int dot = fullName.IndexOf('.', start);
                int end = dot < 0 ? fullName.Length : dot;

                Add(names, seen, fullName.Substring(start, end - start), start == 0 ? string.Empty : fullName.Substring(0, start - 1));

                if (dot < 0)
                {
                    return;
                }

                start = dot + 1;
            }
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

        private static void Add(List<DeclaredName> names, HashSet<string> seen, string name, string namespaceName)
        {
            if (string.IsNullOrEmpty(name))
            {
                return;
            }

            if (seen.Add(namespaceName + "|" + name))
            {
                names.Add(new DeclaredName(name, namespaceName));
            }
        }

        private static void AddNamespace(List<string> namespaces, HashSet<string> seen, string fullName)
        {
            if (seen.Add(fullName))
            {
                namespaces.Add(fullName);
            }
        }

        /// <summary>What a brace has to restore when it closes.</summary>
        private readonly struct NamespaceFrame
        {
            internal NamespaceFrame(bool changedNamespace, string previous)
            {
                ChangedNamespace = changedNamespace;
                Previous = previous;
            }

            internal bool ChangedNamespace { get; }

            internal string Previous { get; }
        }
    }
}
