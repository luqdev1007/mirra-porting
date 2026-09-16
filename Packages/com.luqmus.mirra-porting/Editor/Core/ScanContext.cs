using System;
using System.Collections.Generic;

namespace Luqmus.MirraPorting.Core
{
    /// <summary>What every check is given: the project it runs on, the files that were read and
    /// the names those files declare.</summary>
    internal sealed class ScanContext
    {
        internal ScanContext(
            IProjectEnvironment environment,
            IReadOnlyList<SourceFile> files,
            DeclaredTypeIndex declaredTypes)
        {
            if (environment == null)
            {
                throw new ArgumentNullException(nameof(environment));
            }

            if (files == null)
            {
                throw new ArgumentNullException(nameof(files));
            }

            if (declaredTypes == null)
            {
                throw new ArgumentNullException(nameof(declaredTypes));
            }

            Environment = environment;
            Files = files;
            DeclaredTypes = declaredTypes;
        }

        internal IProjectEnvironment Environment { get; }

        /// <summary>Files in scope, ordered by project relative path.</summary>
        internal IReadOnlyList<SourceFile> Files { get; }

        /// <summary>
        /// Names declared anywhere in the project with their namespaces. A game that declares its
        /// own Time makes a match on Time uncertain, but only in files that can see it.
        /// </summary>
        internal DeclaredTypeIndex DeclaredTypes { get; }
    }
}
