using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Luqmus.MirraPorting.Core;
using NUnit.Framework;

namespace Luqmus.MirraPorting.Tests.Core
{
    public class ScanScopeTests
    {
        private string _temp;
        private string _project;
        private string _outsideLocalPackage;
        private FakeProjectEnvironment _environment;

        [SetUp]
        public void SetUp()
        {
            _temp = Path.Combine(Path.GetTempPath(), "MirraPortingTests", Guid.NewGuid().ToString("N"));
            _project = Path.Combine(_temp, "game");

            // Game code.
            WriteFile("game/Assets/Game/Player.cs");
            WriteFile("game/Assets/Game/Notes.txt");
            WriteFile("game/Assets/Game/.backup.cs");
            WriteFile("game/Assets/Editor/Tool.cs");
            WriteFile("game/Assets/Editorial/Copy.cs");
            WriteFile("game/Assets/Unclaimed/Editor/Helper.cs");

            // A copy of MirraSDK dropped into Assets.
            WriteFile("game/Assets/Plugins/MirraSDK/MirraGames.SDK.Common.asmdef",
                "{\"name\": \"MirraGames.SDK.Common\"}");
            WriteFile("game/Assets/Plugins/MirraSDK/Api.cs");
            WriteFile("game/Assets/Plugins/MirraSDK/Internal/Deep.cs");

            // Folders Unity does not import.
            WriteFile("game/Assets/Samples~/Sample.cs");
            WriteFile("game/Assets/.hidden/Hidden.cs");

            // The porting package itself.
            WriteFile("game/Packages/com.luqmus.mirra-porting/Editor/Own.cs");

            // A local package resolved outside the project.
            _outsideLocalPackage = Path.Combine(_temp, "shared", "local-package");
            WriteFile("shared/local-package/Runtime/Lib.cs");

            _environment = new FakeProjectEnvironment(PathUtil.Normalize(_project));
            _environment.AddRoot("Assets", Path.Combine(_project, "Assets"), string.Empty);
            _environment.AddRoot(
                "Packages/com.luqmus.mirra-porting",
                Path.Combine(_project, "Packages", "com.luqmus.mirra-porting"),
                "com.luqmus.mirra-porting");
            _environment.AddRoot("Packages/com.example.local", _outsideLocalPackage, "com.example.local");

            _environment.InPlayerAssembly("Assets/Game/Player.cs");
            _environment.InPlayerAssembly("Assets/Editorial/Copy.cs");
            _environment.InEditorAssembly("Assets/Editor/Tool.cs");
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
        public void Collect_TakesGameSourcesAndSkipsEverythingElse()
        {
            ScanScopeResult result = new ScanScope(_environment).Collect();

            CollectionAssert.AreEqual(
                new[]
                {
                    "Assets/Editor/Tool.cs",
                    "Assets/Editorial/Copy.cs",
                    "Assets/Game/Player.cs",
                    "Assets/Unclaimed/Editor/Helper.cs",
                    "Packages/com.example.local/Runtime/Lib.cs",
                },
                result.Files.Select(f => f.ProjectRelativePath).ToList());
            CollectionAssert.IsEmpty(result.Errors);
        }

        [Test]
        public void Collect_ReportsALocalPackageUnderItsVirtualPackagesPath()
        {
            ScanScopeResult result = new ScanScope(_environment).Collect();

            ScopedFile file = Single(result, "Packages/com.example.local/Runtime/Lib.cs");
            Assert.AreEqual(
                PathUtil.Normalize(Path.Combine(_outsideLocalPackage, "Runtime", "Lib.cs")),
                file.AbsolutePath);
        }

        [Test]
        public void Collect_EditorOnlyComesFromTheAssemblies()
        {
            ScanScopeResult result = new ScanScope(_environment).Collect();

            Assert.IsTrue(Single(result, "Assets/Editor/Tool.cs").EditorOnly);
            Assert.IsFalse(Single(result, "Assets/Game/Player.cs").EditorOnly);
        }

        [Test]
        public void Collect_EditorOnlyFallsBackToTheEditorFolderForFilesNoAssemblyClaims()
        {
            ScanScopeResult result = new ScanScope(_environment).Collect();

            Assert.IsTrue(Single(result, "Assets/Unclaimed/Editor/Helper.cs").EditorOnly);
            Assert.IsFalse(Single(result, "Packages/com.example.local/Runtime/Lib.cs").EditorOnly);
        }

        [Test]
        public void Collect_EditorialIsNotAnEditorFolder()
        {
            ScanScopeResult result = new ScanScope(_environment).Collect();

            Assert.IsFalse(Single(result, "Assets/Editorial/Copy.cs").EditorOnly);
        }

        [Test]
        public void Collect_FileInBothEditorAndPlayerAssembliesIsNotEditorOnly()
        {
            _environment.InEditorAssembly("Assets/Game/Player.cs");

            ScanScopeResult result = new ScanScope(_environment).Collect();

            Assert.IsFalse(Single(result, "Assets/Game/Player.cs").EditorOnly);
        }

        [Test]
        public void Collect_ScansAPackageOnceItIsNoLongerExcluded()
        {
            var scope = new ScanScope(_environment, new[] { "com.romanlee17.mirrasdk5" });

            ScanScopeResult result = scope.Collect();

            CollectionAssert.Contains(
                result.Files.Select(f => f.ProjectRelativePath).ToList(),
                "Packages/com.luqmus.mirra-porting/Editor/Own.cs");
        }

        [Test]
        public void Collect_MissingRootBecomesAnErrorAndDoesNotStopTheScan()
        {
            _environment.AddRoot("Packages/com.example.gone", Path.Combine(_temp, "gone"), "com.example.gone");

            ScanScopeResult result = new ScanScope(_environment).Collect();

            Assert.AreEqual(1, result.Errors.Count);
            Assert.AreEqual("Packages/com.example.gone", result.Errors[0].Path);
            Assert.AreEqual(5, result.Files.Count);
        }

        private static ScopedFile Single(ScanScopeResult result, string projectRelativePath)
        {
            List<ScopedFile> matches = result.Files
                .Where(f => f.ProjectRelativePath == projectRelativePath)
                .ToList();

            Assert.AreEqual(1, matches.Count, "Expected exactly one " + projectRelativePath);
            return matches[0];
        }

        private void WriteFile(string relativePath, string contents = "")
        {
            string full = Path.Combine(_temp, relativePath.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(full));
            File.WriteAllText(full, contents);
        }
    }
}
