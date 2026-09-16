using System;
using System.Collections.Generic;

namespace Luqmus.MirraPorting.Core
{
    /// <summary>
    /// What every check is given: the project it runs on and the files in scope. Step 4 adds the
    /// tokens of each file and the type names the project declares.
    /// </summary>
    internal sealed class ScanContext
    {
        internal ScanContext(IProjectEnvironment environment, IReadOnlyList<ScopedFile> files)
        {
            if (environment == null)
            {
                throw new ArgumentNullException(nameof(environment));
            }

            if (files == null)
            {
                throw new ArgumentNullException(nameof(files));
            }

            Environment = environment;
            Files = files;
        }

        internal IProjectEnvironment Environment { get; }

        /// <summary>Files in scope, ordered by project relative path.</summary>
        internal IReadOnlyList<ScopedFile> Files { get; }
    }
}
