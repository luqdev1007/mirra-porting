using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Luqmus.MirraPorting.Core;
using NUnit.Framework;

namespace Luqmus.MirraPorting.Tests.Core
{
    public class ScanRunnerTests
    {
        private string _temp;
        private FakeProjectEnvironment _environment;

        [SetUp]
        public void SetUp()
        {
            _temp = Path.Combine(Path.GetTempPath(), "MirraPortingTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(_temp, "Assets"));

            _environment = new FakeProjectEnvironment(_temp);
            _environment.AddRoot("Assets", Path.Combine(_temp, "Assets"), string.Empty);
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
        public void Run_ScansEveryFileAndCountsThem()
        {
            Write("Assets/A.cs", "using UnityEngine;\nclass A { void M() { Time.timeScale = 0f; } }");
            Write("Assets/B.cs", "class B { }");

            ScanReport report = Run();

            Assert.AreEqual(2, report.Summary.FilesScanned);
            Assert.AreEqual(1, report.Findings.Count);
            Assert.AreEqual("Assets/A.cs", report.Findings[0].Path);
        }

        [Test]
        public void Run_SummaryCountsAddUpToTheFindings()
        {
            Write("Assets/A.cs", "using UnityEngine;\nclass A { void M() { Time.timeScale = 0f; var x = Time.timeScale; } }");
            Write("Assets/Editor.cs", "using UnityEngine;\n#if UNITY_EDITOR\nclass E { void M() { Cursor.visible = true; } }\n#endif");

            ScanReport report = Run();

            Assert.AreEqual(report.Findings.Count, report.Summary.TotalFindings);
            Assert.AreEqual(1, report.Summary.EditorOnly);
        }

        [Test]
        public void Run_FindingsComeBackSorted()
        {
            Write("Assets/B.cs", "using UnityEngine;\nclass B { void M() { var x = Time.timeScale; } }");
            Write("Assets/A.cs", "using UnityEngine;\nclass A { void M() { Time.timeScale = 0f; } }");

            ScanReport report = Run();

            CollectionAssert.AreEqual(
                new[] { "API.TIMESCALE_WRITE", "API.TIMESCALE_READ" },
                report.Findings.Select(f => f.RuleId).ToList());
        }

        [Test]
        public void Run_TypesDeclaredInOneFileLowerTheConfidenceInAnother()
        {
            Write("Assets/MyTime.cs", "class Time { }");
            Write("Assets/Use.cs", "using UnityEngine;\nclass U { void M() { Time.timeScale = 0f; } }");

            ScanReport report = Run();

            Assert.AreEqual(1, report.Findings.Count);
            Assert.AreEqual(Confidence.Low, report.Findings[0].Confidence);
        }

        [Test]
        public void Run_UnreadableFileBecomesAnInternalErrorWithoutAPosition()
        {
            Write("Assets/A.cs", "class A { }");
            _environment.AddRoot("Packages/com.example.gone", Path.Combine(_temp, "gone"), "com.example.gone");

            ScanReport report = Run();

            Finding failure = report.Findings.Single(f => f.RuleId == "SCAN.INTERNAL_ERROR");
            Assert.AreEqual(0, failure.Line);
            Assert.AreEqual(0, failure.Column);
            Assert.AreEqual("Packages/com.example.gone", failure.Path);
        }

        [Test]
        public void Run_CountsDiagnosticsWithoutTurningThemIntoFindings()
        {
            Write("Assets/A.cs", "class A { void M() { var s = \"abc\n} }");
            Write("Assets/B.cs", "class B { }");

            ScanReport report = Run();

            Assert.AreEqual(1, report.Diagnostics.FilesWithDiagnostics);
            Assert.AreEqual(1, report.Diagnostics.Total);
            CollectionAssert.IsEmpty(report.Findings);
        }

        [Test]
        public void Run_NamesFilesThatWereNotParsedToTheEnd()
        {
            Write("Assets/A.cs", "class A { }\n/* tail");
            Write("Assets/B.cs", "class B { }");

            ScanReport report = Run();

            Assert.AreEqual(1, report.TruncatedFiles.Count);
            Assert.AreEqual("Assets/A.cs", report.TruncatedFiles[0].Path);
            Assert.AreEqual(2, report.TruncatedFiles[0].Line);
            Assert.AreEqual("UnterminatedComment", report.TruncatedFiles[0].Code);
        }

        [Test]
        public void Run_WarnsWhenTheActivePlatformIsNotWebGl()
        {
            Write("Assets/A.cs", "class A { }");
            _environment.ActiveBuildTarget = "StandaloneWindows64";

            ScanReport report = Run();

            Assert.AreEqual(1, report.Notices.Count);
            StringAssert.Contains("StandaloneWindows64", report.Notices[0]);
            StringAssert.Contains("WebGL", report.Notices[0]);
        }

        [Test]
        public void Run_SaysNothingAboutThePlatformOnWebGl()
        {
            Write("Assets/A.cs", "class A { }");

            ScanReport report = Run();

            CollectionAssert.IsEmpty(report.Notices);
        }

        [Test]
        public void Run_TellsWhenMirraSdkSitsInAssetsInsteadOfBeingAPackage()
        {
            Write("Assets/Plugins/MirraSDK/MirraGames.SDK.asmdef", "{\"name\": \"MirraGames.SDK\"}");
            Write("Assets/Plugins/MirraSDK/Api.cs", "class Api { }");
            Write("Assets/A.cs", "class A { }");

            ScanReport report = Run();

            Assert.AreEqual(1, report.Notices.Count);
            StringAssert.Contains("Assets/Plugins/MirraSDK", report.Notices[0]);
            Assert.AreEqual(1, report.Summary.FilesScanned);
        }

        [Test]
        public void Run_ProjectHeaderComesFromTheEnvironment()
        {
            Write("Assets/A.cs", "class A { }");
            _environment.MirraSdkVersion = "5.1.31";

            ScanReport report = Run();

            Assert.AreEqual("2022.3.62f1", report.Project.UnityVersion);
            Assert.AreEqual("WebGL", report.Project.ActiveBuildTarget);
            Assert.AreEqual("5.1.31", report.Project.MirraSdkVersion);
            Assert.AreEqual("com.luqmus.mirra-porting", report.Project.ToolName);
        }

        [Test]
        public void Run_CancelledBeforeTheFirstFileProducesNoReport()
        {
            Write("Assets/A.cs", "using UnityEngine;\nclass A { void M() { Time.timeScale = 0f; } }");

            var options = new ScanOptions(_temp);
            options.IsCancelled = () => true;

            ScanRunResult result = ScanRunner.Run(_environment, options);

            Assert.IsTrue(result.WasCancelled);
            Assert.IsNull(result.Report);
        }

        [Test]
        public void Run_CancelledBeforeTheChecksProducesNoReport()
        {
            Write("Assets/A.cs", "using UnityEngine;\nclass A { void M() { Time.timeScale = 0f; } }");

            int asked = 0;
            var options = new ScanOptions(_temp);
            options.IsCancelled = () =>
            {
                asked++;
                return asked > 1;
            };

            ScanRunResult result = ScanRunner.Run(_environment, options);

            Assert.IsTrue(result.WasCancelled);
            Assert.IsNull(result.Report);
        }

        [Test]
        public void Run_ReportsProgressForFilesAndChecks()
        {
            Write("Assets/A.cs", "class A { }");

            var stages = new List<string>();
            var options = new ScanOptions(_temp);
            options.OnProgress = progress => stages.Add(progress.Stage);

            ScanRunner.Run(_environment, options);

            CollectionAssert.Contains(stages, "Чтение файлов");
            CollectionAssert.Contains(stages, "Проверки");
        }

        [Test]
        public void Run_ReadsUtf8WithAByteOrderMark()
        {
            string absolute = Path.Combine(_temp, "Assets", "Bom.cs");
            File.WriteAllText(
                absolute, "// комментарий\nusing UnityEngine;\nclass A { void M() { Time.timeScale = 0f; } }",
                new System.Text.UTF8Encoding(true));

            ScanReport report = Run();

            Assert.AreEqual(1, report.Findings.Count);
            Assert.AreEqual(3, report.Findings[0].Line);
        }

        [Test]
        public void Run_EmptyProject()
        {
            ScanReport report = Run();

            Assert.AreEqual(0, report.Summary.FilesScanned);
            CollectionAssert.IsEmpty(report.Findings);
            CollectionAssert.IsEmpty(report.TruncatedFiles);
        }

        private ScanReport Run()
        {
            ScanRunResult result = ScanRunner.Run(_environment, new ScanOptions(_temp));

            Assert.IsFalse(result.WasCancelled);
            Assert.IsNotNull(result.Report);
            return result.Report;
        }

        private void Write(string projectRelativePath, string contents)
        {
            string absolute = Path.Combine(_temp, projectRelativePath.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(absolute));
            File.WriteAllText(absolute, contents);
        }
    }
}
