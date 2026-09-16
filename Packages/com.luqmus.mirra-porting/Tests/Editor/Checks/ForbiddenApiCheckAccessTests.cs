using System.Collections.Generic;
using System.Linq;
using Luqmus.MirraPorting.Core;
using NUnit.Framework;

namespace Luqmus.MirraPorting.Tests.Checks
{
    /// <summary>How the member is touched, where the finding points and what it shows.</summary>
    public class ForbiddenApiCheckAccessTests
    {
        [Test]
        public void CompoundAssignmentAndIncrementsAreWrites()
        {
            Assert.AreEqual("API.TIMESCALE_WRITE", CheckFixture.Single("Time.timeScale += 1;").RuleId);
            Assert.AreEqual("API.TIMESCALE_WRITE", CheckFixture.Single("Time.timeScale *= 2;").RuleId);
            Assert.AreEqual("API.TIMESCALE_WRITE", CheckFixture.Single("Time.timeScale ??= 1;").RuleId);
            Assert.AreEqual("API.TIMESCALE_WRITE", CheckFixture.Single("Time.timeScale++;").RuleId);
            Assert.AreEqual("API.TIMESCALE_WRITE", CheckFixture.Single("--Time.timeScale;").RuleId);
        }

        [Test]
        public void ComparisonAndMemberAccessAreReads()
        {
            Assert.AreEqual("API.TIMESCALE_READ", CheckFixture.Single("if (Time.timeScale == 0) { }").RuleId);
            Assert.AreEqual("API.TIMESCALE_READ", CheckFixture.Single("var s = Time.timeScale.ToString();").RuleId);
        }

        [Test]
        public void ExpressionBodiedMemberThatReadsIsARead()
        {
            Assert.AreEqual("API.TIMESCALE_READ", CheckFixture.Single("float S => Time.timeScale;").RuleId);
        }

        [Test]
        public void ExpressionBodiedMemberThatAssignsIsAWrite()
        {
            Assert.AreEqual("API.TIMESCALE_WRITE", CheckFixture.Single("public void Pause() => Time.timeScale = 0;").RuleId);
        }

        [Test]
        public void BothSidesOfAnAssignmentAreReported()
        {
            IReadOnlyList<Finding> findings = CheckFixture.Run("Time.timeScale = Time.timeScale * 2f;");

            CollectionAssert.AreEqual(
                new[] { "API.TIMESCALE_WRITE", "API.TIMESCALE_READ" },
                findings.Select(f => f.RuleId).ToList());
        }

        [Test]
        public void PositionPointsAtTheFirstTokenOfTheChain()
        {
            Finding bare = CheckFixture.Single("class C\n{\n    void M()\n    {\n        Time.timeScale = 0f;\n    }\n}");
            Assert.AreEqual(5, bare.Line);
            Assert.AreEqual(9, bare.Column);

            Finding qualified = CheckFixture.Single("global::UnityEngine.Time.timeScale = 1;");
            Assert.AreEqual(1, qualified.Line);
            Assert.AreEqual(1, qualified.Column);
        }

        [Test]
        public void ChainBrokenOverLinesPointsAtItsFirstLineAndShowsThemAllInTheSnippet()
        {
            Finding finding = CheckFixture.Single("Time\n    .timeScale\n    = 0f;");

            Assert.AreEqual("API.TIMESCALE_WRITE", finding.RuleId);
            Assert.AreEqual(1, finding.Line);
            Assert.AreEqual(1, finding.Column);
            Assert.AreEqual("Time .timeScale = 0f;", finding.Snippet);
        }

        [Test]
        public void SnippetOfASingleLineIsThatLineTrimmed()
        {
            Finding finding = CheckFixture.Single("        Time.timeScale = 0f;");

            Assert.AreEqual("Time.timeScale = 0f;", finding.Snippet);
        }

        [Test]
        public void CodeInsideAnEditorOnlyBranchIsOnlyInformation()
        {
            Finding finding = CheckFixture.Single("#if UNITY_EDITOR\nTime.timeScale = 0f;\n#endif");

            Assert.AreEqual("API.TIMESCALE_WRITE", finding.RuleId);
            Assert.AreEqual(Severity.Info, finding.Severity);
            Assert.IsTrue(finding.EditorOnly);
        }

        [Test]
        public void CodeInAnEditorOnlyFileIsOnlyInformation()
        {
            Finding finding = CheckFixture.Single("Time.timeScale = 0f;", editorOnly: true);

            Assert.AreEqual(Severity.Info, finding.Severity);
            Assert.IsTrue(finding.EditorOnly);
        }

        [Test]
        public void CodeOutsideAnEditorOnlyBranchKeepsItsSeverity()
        {
            Finding finding = CheckFixture.Single("#if UNITY_EDITOR\n#endif\nTime.timeScale = 0f;");

            Assert.AreEqual(Severity.Error, finding.Severity);
            Assert.IsFalse(finding.EditorOnly);
        }

        [Test]
        public void EveryFindingCarriesThePathAndTheRuleText()
        {
            Finding finding = CheckFixture.Single("using UnityEngine;\nTime.timeScale = 0f;");

            Assert.AreEqual("Assets/Test.cs", finding.Path);
            Assert.AreEqual(Categories.ForbiddenApi, finding.Category);
            Assert.AreEqual(Rules.TimeScaleWrite.Message, finding.Message);
            Assert.AreEqual(Rules.TimeScaleWrite.Suggestion, finding.Suggestion);
        }

        [Test]
        public void BrokenSourceIsScannedWithoutThrowing()
        {
            Assert.DoesNotThrow(() => CheckFixture.Run("var s = \"abc\nTime.timeScale = 0f;"));
            Assert.DoesNotThrow(() => CheckFixture.Run("#if\n#else\n#elif\nTime.timeScale = 0f;"));
            Assert.DoesNotThrow(() => CheckFixture.Run("$\"{"));
            Assert.DoesNotThrow(() => CheckFixture.Run("Time."));
            Assert.DoesNotThrow(() => CheckFixture.Run("Time.timeScale"));
            Assert.DoesNotThrow(() => CheckFixture.Run(string.Empty));
        }

        [Test]
        public void ARecoveredStringDoesNotHideTheCodeBelowIt()
        {
            IReadOnlyList<Finding> findings = CheckFixture.Run("var s = \"abc\nTime.timeScale = 0f;");

            Assert.AreEqual(1, findings.Count, CheckFixture.Dump(findings));
            Assert.AreEqual(2, findings[0].Line);
        }

        [Test]
        public void EachFileIsReportedUnderItsOwnPath()
        {
            IReadOnlyList<Finding> findings = CheckFixture.Run(
                CheckFixture.File("Assets/A.cs", "Time.timeScale = 0f;"),
                CheckFixture.File("Assets/B.cs", "Cursor.visible = false;"));

            CollectionAssert.AreEqual(
                new[] { "Assets/A.cs", "Assets/B.cs" },
                findings.Select(f => f.Path).ToList());
        }
    }
}
