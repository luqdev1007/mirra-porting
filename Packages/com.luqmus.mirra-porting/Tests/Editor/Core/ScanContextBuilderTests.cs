using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Luqmus.MirraPorting.Core;
using NUnit.Framework;

namespace Luqmus.MirraPorting.Tests.Core
{
    public class ScanContextBuilderTests
    {
        private string _temp;
        private FakeProjectEnvironment _environment;

        [SetUp]
        public void SetUp()
        {
            _temp = Path.Combine(Path.GetTempPath(), "MirraPortingTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(_temp, "Assets"));
            _environment = new FakeProjectEnvironment(_temp);
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
        public void Build_ReadsEveryFileAndKeepsTheirOrder()
        {
            ScopedFile first = Write("Assets/A.cs", "namespace Game { class Player { } }");
            ScopedFile second = Write("Assets/B.cs", "class Time { }");

            ScanContextBuildResult result = ScanContextBuilder.Build(_environment, new[] { first, second });

            CollectionAssert.IsEmpty(result.Errors);
            CollectionAssert.AreEqual(
                new[] { "Assets/A.cs", "Assets/B.cs" },
                result.Context.Files.Select(f => f.ProjectRelativePath).ToList());
            Assert.AreSame(_environment, result.Context.Environment);
        }

        [Test]
        public void Build_MergesDeclaredNamesWithoutDuplicatesAndKeepsTheirCase()
        {
            ScopedFile first = Write("Assets/A.cs", "class Time { }");
            ScopedFile second = Write("Assets/B.cs", "class Time { }\nclass time { }");

            ScanContextBuildResult result = ScanContextBuilder.Build(_environment, new[] { first, second });

            var names = new List<string>(result.Context.DeclaredNames);
            names.Sort(StringComparer.Ordinal);
            CollectionAssert.AreEqual(new[] { "Time", "time" }, names);
        }

        [Test]
        public void Build_UnreadableFileBecomesAnErrorAndTheRestIsStillRead()
        {
            ScopedFile missing = new ScopedFile(
                "Assets/Gone.cs", PathUtil.Normalize(Path.Combine(_temp, "Assets", "Gone.cs")), false);
            ScopedFile present = Write("Assets/A.cs", "class Player { }");

            ScanContextBuildResult result = ScanContextBuilder.Build(_environment, new[] { missing, present });

            Assert.AreEqual(1, result.Errors.Count);
            Assert.AreEqual("Assets/Gone.cs", result.Errors[0].Path);
            CollectionAssert.AreEqual(
                new[] { "Assets/A.cs" },
                result.Context.Files.Select(f => f.ProjectRelativePath).ToList());
        }

        [Test]
        public void Build_ReadsUtf8WithAByteOrderMark()
        {
            string absolute = Path.Combine(_temp, "Assets", "Bom.cs");
            File.WriteAllText(absolute, "// комментарий\nclass Время { }", new System.Text.UTF8Encoding(true));
            var file = new ScopedFile("Assets/Bom.cs", PathUtil.Normalize(absolute), false);

            ScanContextBuildResult result = ScanContextBuilder.Build(_environment, new[] { file });

            CollectionAssert.IsEmpty(result.Errors);
            CollectionAssert.AreEqual(new[] { "Время" }, result.Context.Files[0].DeclaredNames);
            Assert.AreEqual(2, result.Context.Files[0].Lex.Tokens[0].Line);
        }

        [Test]
        public void Build_EmptyFileListGivesAnEmptyContext()
        {
            ScanContextBuildResult result = ScanContextBuilder.Build(_environment, new ScopedFile[0]);

            CollectionAssert.IsEmpty(result.Context.Files);
            CollectionAssert.IsEmpty(result.Context.DeclaredNames);
            CollectionAssert.IsEmpty(result.Errors);
        }

        private ScopedFile Write(string projectRelativePath, string contents)
        {
            string absolute = Path.Combine(_temp, projectRelativePath.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(absolute));
            File.WriteAllText(absolute, contents);

            return new ScopedFile(projectRelativePath, PathUtil.Normalize(absolute), false);
        }
    }
}
