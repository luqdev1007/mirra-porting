using System.Collections.Generic;

namespace Luqmus.MirraPorting.Core
{
    /// <summary>What reading the project produced: the context for the checks and what failed on the way.</summary>
    internal sealed class ScanContextBuildResult
    {
        internal ScanContextBuildResult(ScanContext context, IReadOnlyList<ScanError> errors)
        {
            Context = context;
            Errors = errors;
        }

        internal ScanContext Context { get; }

        /// <summary>Files that could not be read or parsed. Empty in a healthy project.</summary>
        internal IReadOnlyList<ScanError> Errors { get; }
    }
}
