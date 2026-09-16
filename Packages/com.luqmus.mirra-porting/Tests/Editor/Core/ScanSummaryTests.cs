using System.Collections.Generic;
using Luqmus.MirraPorting.Core;
using NUnit.Framework;

namespace Luqmus.MirraPorting.Tests.Core
{
    public class ScanSummaryTests
    {
        [Test]
        public void FromFindings_CountsBySeverity()
        {
            ScanSummary summary = ScanSummary.FromFindings(
                new List<Finding>
                {
                    Make(Severity.Error, false),
                    Make(Severity.Error, false),
                    Make(Severity.Warning, false),
                    Make(Severity.Info, false),
                },
                filesScanned: 10);

            Assert.AreEqual(2, summary.Errors);
            Assert.AreEqual(1, summary.Warnings);
            Assert.AreEqual(1, summary.Info);
            Assert.AreEqual(0, summary.EditorOnly);
            Assert.AreEqual(10, summary.FilesScanned);
        }

        [Test]
        public void FromFindings_EditorOnlyIsCountedApartFromSeverity()
        {
            ScanSummary summary = ScanSummary.FromFindings(
                new List<Finding>
                {
                    Make(Severity.Info, true),
                    Make(Severity.Error, true),
                    Make(Severity.Error, false),
                },
                filesScanned: 3);

            Assert.AreEqual(1, summary.Errors);
            Assert.AreEqual(0, summary.Warnings);
            Assert.AreEqual(0, summary.Info);
            Assert.AreEqual(2, summary.EditorOnly);
        }

        [Test]
        public void FromFindings_NothingIsCountedTwice()
        {
            var findings = new List<Finding>
            {
                Make(Severity.Error, false),
                Make(Severity.Warning, false),
                Make(Severity.Info, false),
                Make(Severity.Info, true),
                Make(Severity.Error, true),
            };

            ScanSummary summary = ScanSummary.FromFindings(findings, filesScanned: 5);

            Assert.AreEqual(findings.Count, summary.TotalFindings);
        }

        [Test]
        public void FromFindings_EmptyScan()
        {
            ScanSummary summary = ScanSummary.FromFindings(new List<Finding>(), filesScanned: 0);

            Assert.AreEqual(0, summary.TotalFindings);
        }

        private static Finding Make(Severity severity, bool editorOnly)
        {
            return new Finding(
                "API.TEST", Categories.ForbiddenApi, severity, Confidence.High, string.Empty, editorOnly,
                "Assets/A.cs", 1, 1, "snippet", "message", "suggestion");
        }
    }
}
