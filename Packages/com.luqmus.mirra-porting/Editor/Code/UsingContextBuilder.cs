using System;
using System.Collections.Generic;

namespace Luqmus.MirraPorting.Code
{
    /// <summary>
    /// Collects the using directives of a file and keeps using statements out.
    ///
    /// The test is the brace stack: a using is a directive only when every brace around it opened
    /// a namespace. Inside a type or a method there is at least one other brace, which is where
    /// using (var x = …) and using var x = …; live.
    /// </summary>
    internal static class UsingContextBuilder
    {
        internal static UsingContext Build(IReadOnlyList<Token> tokens)
        {
            if (tokens == null || tokens.Count == 0)
            {
                return UsingContext.Empty;
            }

            var namespaces = new List<QualifiedName>();
            var staticUsings = new List<QualifiedName>();
            var aliases = new Dictionary<string, QualifiedName>(StringComparer.Ordinal);
            var braces = new List<bool>();
            bool pendingNamespace = false;

            for (int i = 0; i < tokens.Count; i++)
            {
                Token token = tokens[i];

                if (token.Kind == TokenKind.Punct)
                {
                    if (token.Text == "{")
                    {
                        braces.Add(pendingNamespace);
                        pendingNamespace = false;
                    }
                    else if (token.Text == "}")
                    {
                        if (braces.Count > 0)
                        {
                            braces.RemoveAt(braces.Count - 1);
                        }
                    }
                    else if (token.Text == ";")
                    {
                        // File scoped namespace: no brace will ever follow, so drop the flag
                        // instead of hanging it on the next type declaration.
                        pendingNamespace = false;
                    }

                    continue;
                }

                if (token.Kind != TokenKind.Identifier)
                {
                    continue;
                }

                if (token.Text == "namespace")
                {
                    pendingNamespace = true;
                    continue;
                }

                if (token.Text == "using" && OnlyNamespaceBraces(braces) && !IsPunctAt(tokens, i + 1, "("))
                {
                    i = ReadUsing(tokens, i + 1, namespaces, staticUsings, aliases);
                }
            }

            return new UsingContext(namespaces, staticUsings, aliases);
        }

        /// <summary>Reads one using directive and returns the index of its last token.</summary>
        private static int ReadUsing(
            IReadOnlyList<Token> tokens,
            int index,
            List<QualifiedName> namespaces,
            List<QualifiedName> staticUsings,
            Dictionary<string, QualifiedName> aliases)
        {
            bool isStatic = IsIdentifierAt(tokens, index, "static");
            if (isStatic)
            {
                index++;
            }

            string alias = null;
            if (!isStatic && IsIdentifier(tokens, index) && IsPunctAt(tokens, index + 1, "="))
            {
                alias = tokens[index].Text;
                index += 2;
            }

            QualifiedName name;
            index = ReadQualifiedName(tokens, index, out name);

            if (name != null)
            {
                if (alias != null)
                {
                    // The same alias twice, for example in two branches of an #if: the last one
                    // wins and nothing throws.
                    aliases[alias] = name;
                }
                else if (isStatic)
                {
                    staticUsings.Add(name);
                }
                else
                {
                    namespaces.Add(name);
                }
            }

            return SkipToEndOfDirective(tokens, index);
        }

        /// <summary>
        /// Reads [global ::] Identifier (. Identifier)* and returns the index after it. A name cut
        /// short by a type argument list keeps the parts read so far.
        /// </summary>
        private static int ReadQualifiedName(IReadOnlyList<Token> tokens, int index, out QualifiedName name)
        {
            name = null;
            bool hadGlobal = false;

            if (IsIdentifierAt(tokens, index, "global") && IsPunctAt(tokens, index + 1, "::"))
            {
                hadGlobal = true;
                index += 2;
            }

            if (!IsIdentifier(tokens, index))
            {
                return index;
            }

            var parts = new List<string> { tokens[index].Text };
            index++;

            while (IsPunctAt(tokens, index, ".") && IsIdentifier(tokens, index + 1))
            {
                parts.Add(tokens[index + 1].Text);
                index += 2;
            }

            name = new QualifiedName(parts, hadGlobal);
            return index;
        }

        private static int SkipToEndOfDirective(IReadOnlyList<Token> tokens, int index)
        {
            while (index < tokens.Count && tokens[index].Kind != TokenKind.Directive)
            {
                if (tokens[index].Kind == TokenKind.Punct && tokens[index].Text == ";")
                {
                    return index;
                }

                index++;
            }

            return index - 1;
        }

        private static bool OnlyNamespaceBraces(List<bool> braces)
        {
            foreach (bool isNamespace in braces)
            {
                if (!isNamespace)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsIdentifier(IReadOnlyList<Token> tokens, int index)
        {
            return index >= 0 && index < tokens.Count && tokens[index].Kind == TokenKind.Identifier;
        }

        private static bool IsIdentifierAt(IReadOnlyList<Token> tokens, int index, string text)
        {
            return IsIdentifier(tokens, index) && string.Equals(tokens[index].Text, text, StringComparison.Ordinal);
        }

        private static bool IsPunctAt(IReadOnlyList<Token> tokens, int index, string text)
        {
            return index >= 0 &&
                   index < tokens.Count &&
                   tokens[index].Kind == TokenKind.Punct &&
                   string.Equals(tokens[index].Text, text, StringComparison.Ordinal);
        }
    }
}
