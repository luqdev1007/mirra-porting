using System;
using System.Collections.Generic;
using Luqmus.MirraPorting.Core;
using Luqmus.MirraPorting.Report;
using NUnit.Framework;

namespace Luqmus.MirraPorting.Tests.Report
{
    public class JsonReportTests
    {
        [Test]
        public void RoundTrip_KeepsEveryField()
        {
            ScanReport original = Sample();

            ScanReport parsed = Parse(JsonReport.ToJson(original));

            Assert.AreEqual(original.Project.Path, parsed.Project.Path);
            Assert.AreEqual(original.Project.UnityVersion, parsed.Project.UnityVersion);
            Assert.AreEqual(original.Project.ActiveBuildTarget, parsed.Project.ActiveBuildTarget);
            Assert.AreEqual(original.Project.MirraSdkVersion, parsed.Project.MirraSdkVersion);
            Assert.AreEqual(original.Project.ToolName, parsed.Project.ToolName);
            Assert.AreEqual(original.Project.ToolVersion, parsed.Project.ToolVersion);
            Assert.AreEqual(original.StartedAtUtc, parsed.StartedAtUtc);
            Assert.AreEqual(original.FinishedAtUtc, parsed.FinishedAtUtc);
            Assert.AreEqual(original.DurationMs, parsed.DurationMs);
            CollectionAssert.AreEqual(original.Notices, parsed.Notices);
            Assert.AreEqual(original.Summary.Errors, parsed.Summary.Errors);
            Assert.AreEqual(original.Summary.Warnings, parsed.Summary.Warnings);
            Assert.AreEqual(original.Summary.Info, parsed.Summary.Info);
            Assert.AreEqual(original.Summary.EditorOnly, parsed.Summary.EditorOnly);
            Assert.AreEqual(original.Summary.FilesScanned, parsed.Summary.FilesScanned);
            Assert.AreEqual(original.Diagnostics.FilesWithDiagnostics, parsed.Diagnostics.FilesWithDiagnostics);
            Assert.AreEqual(original.Diagnostics.Total, parsed.Diagnostics.Total);
        }

        [Test]
        public void RoundTrip_KeepsTruncatedFiles()
        {
            ScanReport parsed = Parse(JsonReport.ToJson(Sample()));

            Assert.AreEqual(1, parsed.TruncatedFiles.Count);
            Assert.AreEqual("Assets/Legacy/Old.cs", parsed.TruncatedFiles[0].Path);
            Assert.AreEqual(42, parsed.TruncatedFiles[0].Line);
            Assert.AreEqual("UnterminatedComment", parsed.TruncatedFiles[0].Code);
        }

        [Test]
        public void RoundTrip_KeepsEveryFieldOfAFinding()
        {
            ScanReport parsed = Parse(JsonReport.ToJson(Sample()));
            Finding expected = Sample().Findings[0];
            Finding actual = parsed.Findings[0];

            Assert.AreEqual(expected.RuleId, actual.RuleId);
            Assert.AreEqual(expected.Category, actual.Category);
            Assert.AreEqual(expected.Severity, actual.Severity);
            Assert.AreEqual(expected.Confidence, actual.Confidence);
            Assert.AreEqual(expected.ConfidenceReason, actual.ConfidenceReason);
            Assert.AreEqual(expected.EditorOnly, actual.EditorOnly);
            Assert.AreEqual(expected.Path, actual.Path);
            Assert.AreEqual(expected.Line, actual.Line);
            Assert.AreEqual(expected.Column, actual.Column);
            Assert.AreEqual(expected.Snippet, actual.Snippet);
            Assert.AreEqual(expected.Message, actual.Message);
            Assert.AreEqual(expected.Suggestion, actual.Suggestion);
        }

        [Test]
        public void RoundTrip_SurvivesQuotesBackslashesAndCyrillic()
        {
            const string snippet = "var p = @\"C:\\dir\\\"\"x\"\"\"; // путь";
            ScanReport original = WithFinding(Finding(snippet, "Сообщение с «кавычками» и \\ слэшем"));

            ScanReport parsed = Parse(JsonReport.ToJson(original));

            Assert.AreEqual(snippet, parsed.Findings[0].Snippet);
            Assert.AreEqual("Сообщение с «кавычками» и \\ слэшем", parsed.Findings[0].Message);
        }

        [Test]
        public void RoundTrip_SurvivesALineBreakInsideASnippet()
        {
            ScanReport parsed = Parse(JsonReport.ToJson(WithFinding(Finding("a\nb", "message"))));

            Assert.AreEqual("a\nb", parsed.Findings[0].Snippet);
        }

        [Test]
        public void ToJson_WritesTheSchemaVersionAndTheDatesAsUtcStrings()
        {
            string json = JsonReport.ToJson(Sample());

            StringAssert.Contains("\"schemaVersion\": 1", json);
            StringAssert.Contains("\"startedAt\": \"2026-09-16T12:00:00Z\"", json);
            StringAssert.Contains("\"finishedAt\": \"2026-09-16T12:00:01Z\"", json);
        }

        [Test]
        public void ToJson_EmptyReportKeepsEmptyArrays()
        {
            var report = new ScanReport(
                new ProjectInfo("D:/p", "2022.3.62f1", "WebGL", string.Empty, "com.luqmus.mirra-porting", "0.1.0"),
                new DateTime(2026, 9, 16, 12, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 9, 16, 12, 0, 0, DateTimeKind.Utc),
                0,
                new string[0],
                new ScanSummary(0, 0, 0, 0, 0),
                new DiagnosticsSummary(0, 0),
                new TruncatedFile[0],
                new Finding[0]);

            string json = JsonReport.ToJson(report);
            ScanReport parsed = Parse(json);

            StringAssert.Contains("\"findings\": []", json);
            CollectionAssert.IsEmpty(parsed.Findings);
            CollectionAssert.IsEmpty(parsed.Notices);
            CollectionAssert.IsEmpty(parsed.TruncatedFiles);
        }

        [Test]
        public void TryParse_UnknownSeverityAndConfidenceDoNotBreakReading()
        {
            const string json = "{\"schemaVersion\":1,\"tool\":{\"name\":\"t\",\"version\":\"1\"}," +
                                "\"project\":{\"path\":\"p\"},\"summary\":{\"errors\":0}," +
                                "\"findings\":[{\"ruleId\":\"API.X\",\"severity\":\"Catastrophic\",\"confidence\":\"Absolute\"}]}";

            ScanReport report = Parse(json);

            Assert.AreEqual(Severity.Info, report.Findings[0].Severity);
            Assert.AreEqual(Confidence.Low, report.Findings[0].Confidence);
        }

        [Test]
        public void TryParse_RejectsWhatIsNotAReport()
        {
            ScanReport report;

            Assert.IsFalse(JsonReport.TryParse(null, out report));
            Assert.IsFalse(JsonReport.TryParse(string.Empty, out report));
            Assert.IsFalse(JsonReport.TryParse("{", out report));
            Assert.IsFalse(JsonReport.TryParse("not json", out report));
            Assert.IsNull(report);
        }

        private static ScanReport Parse(string json)
        {
            ScanReport report;

            Assert.IsTrue(JsonReport.TryParse(json, out report), "Could not parse:\n" + json);
            return report;
        }

        private static ScanReport WithFinding(Finding finding)
        {
            ScanReport sample = Sample();
            return new ScanReport(
                sample.Project,
                sample.StartedAtUtc,
                sample.FinishedAtUtc,
                sample.DurationMs,
                sample.Notices,
                sample.Summary,
                sample.Diagnostics,
                sample.TruncatedFiles,
                new List<Finding> { finding });
        }

        private static Finding Finding(string snippet, string message)
        {
            return new Finding(
                "API.TIMESCALE_WRITE", Categories.ForbiddenApi, Severity.Error, Confidence.Low,
                "в проекте объявлен свой тип Time (MyGame)", false, "Assets/A.cs", 42, 9, snippet, message,
                "MirraSDK.Time.Scale");
        }

        private static ScanReport Sample()
        {
            return new ScanReport(
                new ProjectInfo(
                    "D:/ports/game", "2022.3.62f1", "WebGL", "5.1.31", "com.luqmus.mirra-porting", "0.1.0"),
                new DateTime(2026, 9, 16, 12, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 9, 16, 12, 0, 1, DateTimeKind.Utc),
                1234,
                new[] { "Активная платформа — StandaloneWindows64, а не WebGL." },
                new ScanSummary(12, 30, 5, 7, 840),
                new DiagnosticsSummary(3, 11),
                new List<TruncatedFile> { new TruncatedFile("Assets/Legacy/Old.cs", 42, "UnterminatedComment") },
                new List<Finding> { Finding("Time.timeScale = 0f;", "Прямая запись Time.timeScale в обход SDK") });
        }
    }
}
