using System;
using System.Collections.Generic;
using Luqmus.MirraPorting.Code;

namespace Luqmus.MirraPorting.Core
{
    /// <summary>
    /// Every name the project declares, and the namespaces each of them was declared in. A check
    /// asks whether a name is declared somewhere and then decides, per file, whether that
    /// declaration is visible there.
    /// </summary>
    internal sealed class DeclaredTypeIndex
    {
        internal static readonly DeclaredTypeIndex Empty =
            new DeclaredTypeIndex(new Dictionary<string, List<string>>(StringComparer.Ordinal));

        private readonly Dictionary<string, List<string>> _namespacesByName;

        private DeclaredTypeIndex(Dictionary<string, List<string>> namespacesByName)
        {
            _namespacesByName = namespacesByName;
        }

        /// <summary>Declared names, case sensitive like C# identifiers.</summary>
        internal IReadOnlyCollection<string> Names
        {
            get { return _namespacesByName.Keys; }
        }

        internal int Count
        {
            get { return _namespacesByName.Count; }
        }

        internal static DeclaredTypeIndex Build(IEnumerable<DeclaredName> names)
        {
            var byName = new Dictionary<string, List<string>>(StringComparer.Ordinal);

            foreach (DeclaredName declared in names)
            {
                List<string> namespaces;
                if (!byName.TryGetValue(declared.Name, out namespaces))
                {
                    namespaces = new List<string>();
                    byName.Add(declared.Name, namespaces);
                }

                if (!namespaces.Contains(declared.Namespace))
                {
                    namespaces.Add(declared.Namespace);
                }
            }

            return new DeclaredTypeIndex(byName);
        }

        internal bool Contains(string name)
        {
            return name != null && _namespacesByName.ContainsKey(name);
        }

        /// <summary>Namespaces the name is declared in; an empty string means the global namespace.</summary>
        internal bool TryGetNamespaces(string name, out IReadOnlyList<string> namespaces)
        {
            List<string> found;
            if (name != null && _namespacesByName.TryGetValue(name, out found))
            {
                namespaces = found;
                return true;
            }

            namespaces = null;
            return false;
        }
    }
}
