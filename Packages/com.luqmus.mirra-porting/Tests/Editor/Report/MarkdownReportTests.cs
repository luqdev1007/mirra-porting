using System;
using System.Collections.Generic;
using Luqmus.MirraPorting.Core;
using Luqmus.MirraPorting.Report;
using NUnit.Framework;

namespace Luqmus.MirraPorting.Tests.Report
{
    public class MarkdownReportTests
    {
        [Test]
        public void Header_TellsWhatTheScanWasMadeOn()
        {
            string text = Build(Report(Findings()));

            StringAssert.Contains("# Отчёт Mirra Porting", text);
            StringAssert.Contains("- Unity: 2022.3.62f1", text);
            StringAssert.Contains("- Активная платформа: WebGL", text);
            StringAssert.Contains("- MirraSDK: 5.1.31", text);
            StringAssert.Contains("- Просканировано файлов: 840", text);
            StringAssert.Contains("2026-09-16 12:00:01 UTC", text);
        }

        [Test]
        public void Header_SaysWhenTheSdkIsNotInstalledAtAll()
        {
            string text = Build(Report(Findings(), sdkVersion: string.Empty));

            StringAssert.Contains("- MirraSDK: не установлен", text);
        }

        [Test]
        public void Header_SaysWhenTheSdkSitsInAssetsInsteadOfBeingAPackage()
        {
            string text = Build(Report(
                Findings(), sdkVersion: string.Empty, sdkFolders: new[] { "Assets/Plugins/MirraSDK" }));

            StringAssert.Contains("не найден как пакет", text);
            StringAssert.Contains("Assets/Plugins/MirraSDK", text);
        }

        [Test]
        public void Notices_ComeBeforeTheSummary()
        {
            string text = Build(Report(Findings(), notices: new[] { "Активная платформа — StandaloneWindows64." }));

            int notice = text.IndexOf("**Внимание.**", StringComparison.Ordinal);
            int summary = text.IndexOf("## Сводка", StringComparison.Ordinal);

            Assert.Greater(notice, 0);
            Assert.Less(notice, summary);
        }

        [Test]
        public void Summary_LeavesEditorOnlyFindingsOutOfTheTable()
        {
            string text = Build(Report(new List<Finding>
            {
                Finding("API.TIMESCALE_WRITE", Severity.Error, false),
                Finding("API.TIMESCALE_WRITE", Severity.Info, true),
            }));

            StringAssert.Contains("| ForbiddenApi | 1 | 0 | 0 |", text);
            StringAssert.Contains("| **Всего** | 1 | 0 | 0 |", text);
            StringAssert.Contains("в разделе «Только редактор»", text);
        }

        [Test]
        public void Sections_ComeInTheAgreedOrder()
        {
            string text = Build(Report(
                new List<Finding> { Finding("API.TIMESCALE_WRITE", Severity.Error, false), Finding("API.QUIT", Severity.Info, true) },
                truncated: new[] { new TruncatedFile("Assets/Old.cs", 42, "UnterminatedComment") }));

            int findings = text.IndexOf("## Находки", StringComparison.Ordinal);
            int truncated = text.IndexOf("## Разобраны не полностью", StringComparison.Ordinal);
            int editorOnly = text.IndexOf("## Только редактор", StringComparison.Ordinal);

            Assert.Greater(findings, 0);
            Assert.Greater(truncated, findings);
            Assert.Greater(editorOnly, truncated);
        }

        [Test]
        public void Findings_AreGroupedByRuleWithTheExplanationOnce()
        {
            string text = Build(Report(new List<Finding>
            {
                Finding("API.TIMESCALE_WRITE", Severity.Error, false, path: "Assets/A.cs", line: 1),
                Finding("API.TIMESCALE_WRITE", Severity.Error, false, path: "Assets/B.cs", line: 2),
            }));

            StringAssert.Contains("#### API.TIMESCALE_WRITE — 2", text);
            StringAssert.Contains("Замена: MirraSDK.Time.Scale", text);
            StringAssert.Contains("- `Assets/A.cs:1` — `Time.timeScale = 0f;`", text);
            StringAssert.Contains("- `Assets/B.cs:2` — `Time.timeScale = 0f;`", text);
        }

        [Test]
        public void Finding_WithoutALineIsWrittenWithoutOne()
        {
            string text = Build(Report(new List<Finding>
            {
                new Finding(
                    "SCAN.INTERNAL_ERROR", Categories.Scan, Severity.Warning, Confidence.High, string.Empty, false,
                    "Assets/Gone.cs", 0, 0, string.Empty, "Не удалось прочитать файл", "Сообщите разработчику"),
            }));

            StringAssert.Contains("- `Assets/Gone.cs` — Не удалось прочитать файл", text);
            StringAssert.DoesNotContain("Assets/Gone.cs:0", text);
        }

        [Test]
        public void Finding_WithoutAPathIsAboutTheWholeProject()
        {
            string text = Build(Report(new List<Finding>
            {
                new Finding(
                    "SCAN.INTERNAL_ERROR", Categories.Scan, Severity.Warning, Confidence.High, string.Empty, false,
                    string.Empty, 0, 0, string.Empty, "Проверка упала", "Сообщите разработчику"),
            }));

            StringAssert.Contains("- весь проект — Проверка упала", text);
        }

        [Test]
        public void Snippet_WithABacktickGetsALongerFence()
        {
            string text = Build(Report(new List<Finding>
            {
                Finding("API.TIMESCALE_WRITE", Severity.Error, false, snippet: "var s = `x`;"),
            }));

            StringAssert.Contains("— `` var s = `x`; ``", text);
        }

        [Test]
        public void LowConfidence_IsMarkedWithItsReason()
        {
            string text = Build(Report(new List<Finding>
            {
                new Finding(
                    "API.TIMESCALE_WRITE", Categories.ForbiddenApi, Severity.Error, Confidence.Low,
                    "в проекте объявлен свой тип Time (MyGame)", false, "Assets/A.cs", 1, 1, "Time.timeScale = 0f;",
                    "message", "MirraSDK.Time.Scale"),
            }));

            StringAssert.Contains("(low confidence: в проекте объявлен свой тип Time (MyGame))", text);
        }

        [Test]
        public void MediumConfidence_IsMarkedToo()
        {
            string text = Build(Report(new List<Finding>
            {
                new Finding(
                    "API.TIMESCALE_WRITE", Categories.ForbiddenApi, Severity.Error, Confidence.Medium,
                    "в файле нет using UnityEngine", false, "Assets/A.cs", 1, 1, "Time.timeScale = 0f;",
                    "message", "MirraSDK.Time.Scale"),
            }));

            StringAssert.Contains("(low confidence: в файле нет using UnityEngine)", text);
        }

        [Test]
        public void HighConfidence_IsNotMarked()
        {
            string text = Build(Report(Findings()));

            StringAssert.DoesNotContain("low confidence", text);
        }

        [Test]
        public void EmptyReport_SaysSoAndHasNoEmptySections()
        {
            string text = Build(Report(new List<Finding>()));

            StringAssert.Contains("В коде, который попадает в сборку игры, находок нет.", text);
            StringAssert.DoesNotContain("## Находки", text);
            StringAssert.DoesNotContain("## Только редактор", text);
            StringAssert.DoesNotContain("## Разобраны не полностью", text);
        }

        [Test]
        public void Counts_AgreeWithTheNounAfterThem()
        {
            StringAssert.Contains(
                "Ещё 1 находка в коде",
                Build(Report(new List<Finding>
                {
                    Finding("API.TIMESCALE_WRITE", Severity.Error, false),
                    Finding("API.QUIT", Severity.Info, true),
                })));

            var many = new List<Finding> { Finding("API.TIMESCALE_WRITE", Severity.Error, false) };
            for (int i = 0; i < 5; i++)
            {
                many.Add(Finding("API.QUIT", Severity.Info, true));
            }

            StringAssert.Contains("Ещё 5 находок в коде", Build(Report(many)));
            StringAssert.Contains(
                "в 1 файле", Build(Report(Findings(), diagnostics: new DiagnosticsSummary(1, 2))));
        }

        [Test]
        public void Diagnostics_AreMentionedOnlyWhenThereAreSome()
        {
            StringAssert.DoesNotContain("не удалось разобрать", Build(Report(Findings())));
            StringAssert.Contains(
                "Мест, которые не удалось разобрать: 11 в 3 файлах",
                Build(Report(Findings(), diagnostics: new DiagnosticsSummary(3, 11))));
        }

        private static string Build(ScanReport report)
        {
            return MarkdownReport.Build(report);
        }

        private static List<Finding> Findings()
        {
            return new List<Finding> { Finding("API.TIMESCALE_WRITE", Severity.Error, false) };
        }

        private static Finding Finding(
            string ruleId,
            Severity severity,
            bool editorOnly,
            string path = "Assets/A.cs",
            int line = 42,
            string snippet = "Time.timeScale = 0f;")
        {
            return new Finding(
                ruleId,
                ruleId.StartsWith("SCAN.", StringComparison.Ordinal) ? Categories.Scan : Categories.ForbiddenApi,
                severity,
                Confidence.High,
                string.Empty,
                editorOnly,
                path,
                line,
                9,
                snippet,
                "Прямая запись Time.timeScale в обход SDK",
                "MirraSDK.Time.Scale");
        }

        private static ScanReport Report(
            IReadOnlyList<Finding> findings,
            string sdkVersion = "5.1.31",
            IReadOnlyList<string> notices = null,
            IReadOnlyList<string> sdkFolders = null,
            IReadOnlyList<TruncatedFile> truncated = null,
            DiagnosticsSummary diagnostics = default(DiagnosticsSummary))
        {
            return new ScanReport(
                new ProjectInfo(
                    "D:/ports/game", "2022.3.62f1", "WebGL", sdkVersion, "com.luqmus.mirra-porting", "0.1.0"),
                new DateTime(2026, 9, 16, 12, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 9, 16, 12, 0, 1, DateTimeKind.Utc),
                1234,
                notices,
                ScanSummary.FromFindings(findings, 840),
                diagnostics,
                truncated,
                findings,
                sdkFolders);
        }
    }
}
