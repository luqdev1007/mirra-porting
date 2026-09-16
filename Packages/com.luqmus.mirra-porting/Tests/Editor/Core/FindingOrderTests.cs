using System.Collections.Generic;
using System.Linq;
using Luqmus.MirraPorting.Core;
using NUnit.Framework;

namespace Luqmus.MirraPorting.Tests.Core
{
    public class FindingOrderTests
    {
        [Test]
        public void Sort_OrdersBySeverityThenCategoryRulePathLine()
        {
            var findings = new List<Finding>
            {
                Make(Severity.Warning, "ForbiddenApi", "API.B", "Assets/A.cs", 1),
                Make(Severity.Error, "ForbiddenApi", "API.B", "Assets/A.cs", 5),
                Make(Severity.Error, "ForbiddenApi", "API.B", "Assets/A.cs", 2),
                Make(Severity.Error, "ForbiddenApi", "API.B", "Assets/B.cs", 1),
                Make(Severity.Error, "ForbiddenApi", "API.A", "Assets/Z.cs", 9),
                Make(Severity.Error, "Scan", "SCAN.INTERNAL_ERROR", "Assets/A.cs", 1),
                Make(Severity.Info, "ForbiddenApi", "API.A", "Assets/A.cs", 1),
            };

            IReadOnlyList<Finding> sorted = FindingOrder.Sort(findings);

            var keys = sorted.Select(f => f.Severity + "|" + f.Category + "|" + f.RuleId + "|" + f.Path + "|" + f.Line);
            CollectionAssert.AreEqual(
                new[]
                {
                    "Error|ForbiddenApi|API.A|Assets/Z.cs|9",
                    "Error|ForbiddenApi|API.B|Assets/A.cs|2",
                    "Error|ForbiddenApi|API.B|Assets/A.cs|5",
                    "Error|ForbiddenApi|API.B|Assets/B.cs|1",
                    "Error|Scan|SCAN.INTERNAL_ERROR|Assets/A.cs|1",
                    "Warning|ForbiddenApi|API.B|Assets/A.cs|1",
                    "Info|ForbiddenApi|API.A|Assets/A.cs|1",
                },
                keys.ToList());
        }

        [Test]
        public void Sort_IsStableForFindingsThatTieOnEveryKey()
        {
            var first = Make(Severity.Error, "ForbiddenApi", "API.A", "Assets/A.cs", 1, column: 30);
            var second = Make(Severity.Error, "ForbiddenApi", "API.A", "Assets/A.cs", 1, column: 10);

            IReadOnlyList<Finding> sorted = FindingOrder.Sort(new[] { first, second });

            Assert.AreSame(first, sorted[0]);
            Assert.AreSame(second, sorted[1]);
        }

        [Test]
        public void Collector_SortedReturnsReportOrderAndAsAddedKeepsInput()
        {
            var collector = new FindingCollector();
            var warning = Make(Severity.Warning, "ForbiddenApi", "API.A", "Assets/A.cs", 1);
            var error = Make(Severity.Error, "ForbiddenApi", "API.A", "Assets/A.cs", 2);
            collector.Add(warning);
            collector.Add(error);

            Assert.AreEqual(2, collector.Count);
            CollectionAssert.AreEqual(new[] { warning, error }, collector.AsAdded().ToList());
            CollectionAssert.AreEqual(new[] { error, warning }, collector.Sorted().ToList());
        }

        private static Finding Make(
            Severity severity, string category, string ruleId, string path, int line, int column = 1)
        {
            return new Finding(
                ruleId, category, severity, Confidence.High, string.Empty, false, path, line, column, "snippet", "message", "suggestion");
        }
    }
}
