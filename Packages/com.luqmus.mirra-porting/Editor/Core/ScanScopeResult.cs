using System.Collections.Generic;

namespace Luqmus.MirraPorting.Core
{
    /// <summary>What walking the project produced: the files to scan and what went wrong on the way.</summary>
    internal sealed class ScanScopeResult
    {
        internal ScanScopeResult(IReadOnlyList<ScopedFile> files, IReadOnlyList<ScanError> errors)
        {
            Files = files;
            Errors = errors;
        }

        /// <summary>Files ordered by project relative path, so two scans produce the same report.</summary>
        internal IReadOnlyList<ScopedFile> Files { get; }

        /// <summary>Folders that could not be read. Empty in a healthy project.</summary>
        internal IReadOnlyList<ScanError> Errors { get; }
    }
}
