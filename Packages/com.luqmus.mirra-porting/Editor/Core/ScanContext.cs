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
            IReadOnlyCollection<string> declaredNames)
        {
            if (environment == null)
            {
                throw new ArgumentNullException(nameof(environment));
            }

            if (files == null)
            {
                throw new ArgumentNullException(nameof(files));
            }

            if (declaredNames == null)
            {
                throw new ArgumentNullException(nameof(declaredNames));
            }

            Environment = environment;
            Files = files;
            DeclaredNames = declaredNames;
        }

        internal IProjectEnvironment Environment { get; }

        /// <summary>Files in scope, ordered by project relative path.</summary>
        internal IReadOnlyList<SourceFile> Files { get; }

        /// <summary>
        /// Type and namespace names declared anywhere in the project, case sensitive. A game that
        /// declares its own Time makes a match on Time uncertain.
        /// </summary>
        internal IReadOnlyCollection<string> DeclaredNames { get; }
    }
}
