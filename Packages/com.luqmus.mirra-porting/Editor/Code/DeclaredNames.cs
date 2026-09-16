using System.Collections.Generic;

namespace Luqmus.MirraPorting.Code
{
    /// <summary>What one file declares: its names and the namespaces it opens.</summary>
    internal sealed class DeclaredNames
    {
        internal static readonly DeclaredNames Empty = new DeclaredNames(new DeclaredName[0], new string[0]);

        internal DeclaredNames(IReadOnlyList<DeclaredName> names, IReadOnlyList<string> namespaces)
        {
            Names = names;
            Namespaces = namespaces;
        }

        /// <summary>Type names and namespace segments, each with the namespace it sits in.</summary>
        internal IReadOnlyList<DeclaredName> Names { get; }

        /// <summary>
        /// Full names of the namespaces this file declares, for example "Game.Ui". Used to decide
        /// which declarations are visible in the file without a using.
        /// </summary>
        internal IReadOnlyList<string> Namespaces { get; }
    }
}
