using System.Collections.Generic;
using System.Linq;
using Luqmus.MirraPorting.Checks;
using Luqmus.MirraPorting.Code;
using Luqmus.MirraPorting.Core;
using Luqmus.MirraPorting.Tests.Core;
using NUnit.Framework;

namespace Luqmus.MirraPorting.Tests.Checks
{
    /// <summary>
    /// Runs a check over sources held in memory. Nothing here touches the disk: a scanned file is
    /// a path and a string.
    /// </summary>
    internal static class CheckFixture
    {
        internal static SourceFile File(string projectRelativePath, string text, bool editorOnly = false)
        {
            var scoped = new ScopedFile(projectRelativePath, "D:/project/" + projectRelativePath, editorOnly);
            return SourceFile.FromText(scoped, text);
        }

        internal static IReadOnlyList<Finding> Run(params SourceFile[] files)
        {
            var declared = new List<DeclaredName>();
            foreach (SourceFile file in files)
            {
                declared.AddRange(file.Declarations.Names);
            }

            var context = new ScanContext(
                new FakeProjectEnvironment("D:/project"), files, DeclaredTypeIndex.Build(declared));

            var collector = new FindingCollector();
            new ForbiddenApiCheck().Run(context, collector);
            return collector.AsAdded();
        }

        /// <summary>Runs the check over one file of game code.</summary>
        internal static IReadOnlyList<Finding> Run(string source, bool editorOnly = false)
        {
            return Run(File("Assets/Test.cs", source, editorOnly));
        }

        /// <summary>Runs over one file and asserts it produced exactly one finding.</summary>
        internal static Finding Single(string source, bool editorOnly = false)
        {
            IReadOnlyList<Finding> findings = Run(source, editorOnly);

            Assert.AreEqual(1, findings.Count, "Expected one finding, got:\n" + Dump(findings) + "\nin:\n" + source);
            return findings[0];
        }

        /// <summary>Runs over one file and asserts it found nothing.</summary>
        internal static void None(string source)
        {
            IReadOnlyList<Finding> findings = Run(source);

            Assert.AreEqual(0, findings.Count, "Expected nothing, got:\n" + Dump(findings) + "\nin:\n" + source);
        }

        internal static string Dump(IReadOnlyList<Finding> findings)
        {
            return string.Join(
                "\n",
                findings.Select(f => "  " + f.RuleId + " " + f.Confidence + " at " + f.Path + ":" + f.Line).ToArray());
        }
    }
}
