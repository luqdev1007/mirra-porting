using System;
using System.Collections.Generic;

namespace Luqmus.MirraPorting.Core
{
    /// <summary>
    /// Everything one scan produced, ready to be written as JSON or Markdown. Nothing here knows
    /// about either format.
    /// </summary>
    internal sealed class ScanReport
    {
        internal ScanReport(
            ProjectInfo project,
            DateTime startedAtUtc,
            DateTime finishedAtUtc,
            long durationMs,
            IReadOnlyList<string> notices,
            ScanSummary summary,
            DiagnosticsSummary diagnostics,
            IReadOnlyList<TruncatedFile> truncatedFiles,
            IReadOnlyList<Finding> findings)
        {
            if (project == null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            if (findings == null)
            {
                throw new ArgumentNullException(nameof(findings));
            }

            Project = project;
            StartedAtUtc = startedAtUtc;
            FinishedAtUtc = finishedAtUtc;
            DurationMs = durationMs;
            Notices = notices ?? new string[0];
            Summary = summary;
            Diagnostics = diagnostics;
            TruncatedFiles = truncatedFiles ?? new TruncatedFile[0];
            Findings = findings;
        }

        internal ProjectInfo Project { get; }

        internal DateTime StartedAtUtc { get; }

        internal DateTime FinishedAtUtc { get; }

        internal long DurationMs { get; }

        /// <summary>Things the reader has to know before trusting the numbers, in Russian.</summary>
        internal IReadOnlyList<string> Notices { get; }

        internal ScanSummary Summary { get; }

        internal DiagnosticsSummary Diagnostics { get; }

        internal IReadOnlyList<TruncatedFile> TruncatedFiles { get; }

        /// <summary>Findings in report order, see <see cref="FindingOrder"/>.</summary>
        internal IReadOnlyList<Finding> Findings { get; }
    }
}
