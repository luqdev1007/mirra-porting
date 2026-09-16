using System;
using System.IO;
using System.Text.RegularExpressions;
using Luqmus.MirraPorting;
using Luqmus.MirraPorting.Core;
using Luqmus.MirraPorting.Report;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Luqmus.MirraPorting.Tests.Report
{
    /// <summary>
    /// The public entry point, run against the real project. These are the only tests that scan
    /// the whole repository, so they check the shape of the result rather than its contents.
    /// </summary>
    public class MirraPortingScannerTests
    {
        private string _temp;

        [SetUp]
        public void SetUp()
        {
            _temp = Path.Combine(Path.GetTempPath(), "MirraPortingTests", Guid.NewGuid().ToString("N"));
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_temp))
            {
                Directory.Delete(_temp, true);
            }
        }

        [Test]
        public void ScanAndExport_WritesAReportThatReadsBack()
        {
            string path = MirraPortingScanner.ScanAndExport(_temp);

            Assert.IsNotNull(path);
            Assert.IsTrue(File.Exists(path));

            ScanReport report;
            Assert.IsTrue(JsonReport.TryParse(File.ReadAllText(path), out report));
            Assert.AreEqual(report.Findings.Count, report.Summary.TotalFindings);
            Assert.Greater(report.Summary.FilesScanned, 0);
        }

        [Test]
        public void ScanAndExport_TwoScansOfAnUnchangedProjectDifferOnlyInTiming()
        {
            string first = File.ReadAllText(MirraPortingScanner.ScanAndExport(_temp));
            string second = File.ReadAllText(MirraPortingScanner.ScanAndExport(_temp));

            Assert.AreEqual(WithoutTiming(first), WithoutTiming(second));
        }

        [Test]
        public void ScanAndExport_ReportsNothingFromTheScannerItself()
        {
            ScanReport report;
            Assert.IsTrue(JsonReport.TryParse(File.ReadAllText(MirraPortingScanner.ScanAndExport(_temp)), out report));

            foreach (Finding finding in report.Findings)
            {
                StringAssert.DoesNotContain("Packages/com.luqmus.mirra-porting", finding.Path);
            }
        }

        [Test]
        public void ScanAndExport_AFolderInTheWayOfTheReportIsLoggedAndGivesNull()
        {
            Directory.CreateDirectory(Path.Combine(_temp, ReportWriter.JsonFileName));
            LogAssert.Expect(LogType.Error, new Regex("Не удалось записать отчёт"));

            string path = MirraPortingScanner.ScanAndExport(_temp);

            Assert.IsNull(path);
        }

        /// <summary>Everything but the three fields that are supposed to differ between runs.</summary>
        private static string WithoutTiming(string json)
        {
            string result = Regex.Replace(json, "\"startedAt\": \"[^\"]*\"", "\"startedAt\": \"\"");
            result = Regex.Replace(result, "\"finishedAt\": \"[^\"]*\"", "\"finishedAt\": \"\"");
            return Regex.Replace(result, "\"durationMs\": \\d+", "\"durationMs\": 0");
        }
    }
}
