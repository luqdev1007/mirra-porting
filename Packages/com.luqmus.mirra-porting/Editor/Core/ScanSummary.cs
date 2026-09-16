using System.Collections.Generic;

namespace Luqmus.MirraPorting.Core
{
    /// <summary>
    /// How many findings of each kind the scan produced. Editor only findings are counted on their
    /// own and not by severity, so nothing is counted twice: errors + warnings + info + editorOnly
    /// is exactly the number of findings.
    /// </summary>
    internal readonly struct ScanSummary
    {
        internal ScanSummary(int errors, int warnings, int info, int editorOnly, int filesScanned)
        {
            Errors = errors;
            Warnings = warnings;
            Info = info;
            EditorOnly = editorOnly;
            FilesScanned = filesScanned;
        }

        /// <summary>Errors in code that ships with the game.</summary>
        internal int Errors { get; }

        internal int Warnings { get; }

        internal int Info { get; }

        /// <summary>Findings in code the player build drops, whatever their severity.</summary>
        internal int EditorOnly { get; }

        internal int FilesScanned { get; }

        internal int TotalFindings
        {
            get { return Errors + Warnings + Info + EditorOnly; }
        }

        internal static ScanSummary FromFindings(IReadOnlyList<Finding> findings, int filesScanned)
        {
            int errors = 0;
            int warnings = 0;
            int info = 0;
            int editorOnly = 0;

            foreach (Finding finding in findings)
            {
                if (finding.EditorOnly)
                {
                    editorOnly++;
                    continue;
                }

                switch (finding.Severity)
                {
                    case Severity.Error:
                        errors++;
                        break;
                    case Severity.Warning:
                        warnings++;
                        break;
                    default:
                        info++;
                        break;
                }
            }

            return new ScanSummary(errors, warnings, info, editorOnly, filesScanned);
        }
    }
}
