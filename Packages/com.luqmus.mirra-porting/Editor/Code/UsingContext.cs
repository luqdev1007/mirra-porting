using System;
using System.Collections.Generic;

namespace Luqmus.MirraPorting.Code
{
    /// <summary>
    /// The using directives of one file. The context covers the whole file, without namespace
    /// scopes: a using written inside namespace A { } counts everywhere in the file.
    /// </summary>
    internal sealed class UsingContext
    {
        internal static readonly UsingContext Empty = new UsingContext(
            new QualifiedName[0],
            new QualifiedName[0],
            new Dictionary<string, QualifiedName>(StringComparer.Ordinal));

        internal UsingContext(
            IReadOnlyList<QualifiedName> namespaces,
            IReadOnlyList<QualifiedName> staticUsings,
            IReadOnlyDictionary<string, QualifiedName> aliases)
        {
            Namespaces = namespaces;
            StaticUsings = staticUsings;
            Aliases = aliases;
        }

        /// <summary>using UnityEngine;</summary>
        internal IReadOnlyList<QualifiedName> Namespaces { get; }

        /// <summary>using static UnityEngine.Time;</summary>
        internal IReadOnlyList<QualifiedName> StaticUsings { get; }

        /// <summary>using T = UnityEngine.Time; keyed by the alias, case sensitive like C#.</summary>
        internal IReadOnlyDictionary<string, QualifiedName> Aliases { get; }

        internal bool UsesNamespace(string dotted)
        {
            foreach (QualifiedName name in Namespaces)
            {
                if (string.Equals(name.Dotted, dotted, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
