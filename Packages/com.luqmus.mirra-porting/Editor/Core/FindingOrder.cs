using System;
using System.Collections.Generic;
using System.Linq;

namespace Luqmus.MirraPorting.Core
{
    /// <summary>
    /// Report order of findings: severity, category, rule, path, line. OrderBy is stable, so
    /// findings that tie on every key keep the order the checks produced them in — two runs over
    /// an unchanged project give byte identical reports.
    /// </summary>
    internal static class FindingOrder
    {
        internal static IReadOnlyList<Finding> Sort(IEnumerable<Finding> findings)
        {
            if (findings == null)
            {
                throw new ArgumentNullException(nameof(findings));
            }

            return findings
                .OrderBy(f => f.Severity)
                .ThenBy(f => f.Category, StringComparer.Ordinal)
                .ThenBy(f => f.RuleId, StringComparer.Ordinal)
                .ThenBy(f => f.Path, StringComparer.OrdinalIgnoreCase)
                .ThenBy(f => f.Line)
                .ToList();
        }
    }
}
