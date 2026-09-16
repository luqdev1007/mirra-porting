using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Luqmus.MirraPorting;
using NUnit.Framework;
using UnityEngine;

namespace Sandbox.Tests
{
    /// <summary>
    /// The sandbox check the whole scanner is judged by: run it the way an agent does, read the
    /// report the way a tool does, and compare with what the sandbox sources say they expect.
    ///
    /// This lives in Assets rather than in the package: it tests the package from outside, through
    /// its public entry point only, and it is not copied into a port.
    /// </summary>
    public class SandboxScanTests
    {
        private string _temp;

        [SetUp]
        public void SetUp()
        {
            _temp = Path.Combine(Path.GetTempPath(), "MirraPortingSandbox", Guid.NewGuid().ToString("N"));
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
        public void Scan_FindsExactlyWhatTheSandboxExpects()
        {
            SandboxReportDto report = Scan();

            SortedSet<string> expected = SandboxExpectations.Read();
            SortedSet<string> actual = FindingsInSandbox(report);

            Assert.Greater(expected.Count, 0, "No EXPECT comments found in Assets/Sandbox");

            var missing = new SortedSet<string>(expected, StringComparer.Ordinal);
            missing.ExceptWith(actual);

            var unexpected = new SortedSet<string>(actual, StringComparer.Ordinal);
            unexpected.ExceptWith(expected);

            Assert.IsTrue(
                missing.Count == 0 && unexpected.Count == 0,
                Describe("Ожидалось, но сканер не нашёл", missing) +
                Describe("Сканер нашёл, но не ожидалось", unexpected));
        }

        [Test]
        public void Scan_ReportsNothingFromTheScannerItself()
        {
            SandboxReportDto report = Scan();

            foreach (SandboxFindingDto finding in report.findings)
            {
                StringAssert.DoesNotContain("Packages/com.luqmus.mirra-porting", finding.path);
            }
        }

        [Test]
        public void IsInSandbox_TakesTheSandboxAndNothingThatMerelyStartsLikeIt()
        {
            Assert.IsTrue(SandboxExpectations.IsInSandbox("Assets/Sandbox/ForbiddenApi/A.cs"));
            Assert.IsFalse(SandboxExpectations.IsInSandbox("Assets/SandboxTests/X.cs"));
            Assert.IsFalse(SandboxExpectations.IsInSandbox("Assets/Game/Sandbox/A.cs"));
            Assert.IsFalse(SandboxExpectations.IsInSandbox(null));
        }

        private SandboxReportDto Scan()
        {
            string path = MirraPortingScanner.ScanAndExport(_temp);

            Assert.IsNotNull(path, "The scan produced no report");
            return JsonUtility.FromJson<SandboxReportDto>(File.ReadAllText(path));
        }

        private static SortedSet<string> FindingsInSandbox(SandboxReportDto report)
        {
            var findings = new SortedSet<string>(StringComparer.Ordinal);

            foreach (SandboxFindingDto finding in report.findings)
            {
                if (SandboxExpectations.IsInSandbox(finding.path))
                {
                    findings.Add(SandboxExpectations.Key(
                        finding.path, finding.line, finding.ruleId, finding.severity, finding.confidence,
                        finding.editorOnly));
                }
            }

            return findings;
        }

        private static string Describe(string title, SortedSet<string> lines)
        {
            if (lines.Count == 0)
            {
                return string.Empty;
            }

            var text = new StringBuilder();
            text.AppendLine();
            text.AppendLine(title + " (" + lines.Count + "):");

            foreach (string line in lines)
            {
                text.AppendLine("  " + line);
            }

            return text.ToString();
        }
    }
}
