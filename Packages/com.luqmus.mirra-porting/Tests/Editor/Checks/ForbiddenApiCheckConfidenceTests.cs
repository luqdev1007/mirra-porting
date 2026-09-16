using System.Collections.Generic;
using Luqmus.MirraPorting.Core;
using NUnit.Framework;

namespace Luqmus.MirraPorting.Tests.Checks
{
    /// <summary>How sure the check is, and why it says so.</summary>
    public class ForbiddenApiCheckConfidenceTests
    {
        [Test]
        public void UsingUnityEngineMakesABareNameCertain()
        {
            Finding finding = CheckFixture.Single("using UnityEngine;\nTime.timeScale = 0f;");

            Assert.AreEqual(Confidence.High, finding.Confidence);
            Assert.AreEqual(string.Empty, finding.ConfidenceReason);
        }

        [Test]
        public void WithoutUsingUnityEngineABareNameIsOnlyLikely()
        {
            Finding finding = CheckFixture.Single("Time.timeScale = 0f;");

            Assert.AreEqual(Confidence.Medium, finding.Confidence);
            Assert.AreEqual("в файле нет using UnityEngine", finding.ConfidenceReason);
        }

        [Test]
        public void QualifiedNameIsCertainEvenWhenTheProjectDeclaresItsOwnType()
        {
            IReadOnlyList<Finding> findings = CheckFixture.Run(
                CheckFixture.File("Assets/MyCursor.cs", "class Cursor { }"),
                CheckFixture.File("Assets/Use.cs", "UnityEngine.Cursor.visible = true;"));

            Assert.AreEqual(1, findings.Count, CheckFixture.Dump(findings));
            Assert.AreEqual(Confidence.High, findings[0].Confidence);
        }

        [Test]
        public void ATypeOfTheSameNameInTheGlobalNamespaceShadowsEverywhere()
        {
            IReadOnlyList<Finding> findings = CheckFixture.Run(
                CheckFixture.File("Assets/MyCursor.cs", "class Cursor { }"),
                CheckFixture.File("Assets/Use.cs", "using UnityEngine;\nCursor.visible = true;"));

            Assert.AreEqual(1, findings.Count, CheckFixture.Dump(findings));
            Assert.AreEqual(Confidence.Low, findings[0].Confidence);
            Assert.AreEqual("в проекте объявлен свой тип Cursor (глобальный namespace)", findings[0].ConfidenceReason);
        }

        [Test]
        public void ATypeInItsOwnNamespaceDoesNotShadowAFileThatCannotSeeIt()
        {
            IReadOnlyList<Finding> findings = CheckFixture.Run(
                CheckFixture.File("Assets/MyCursor.cs", "namespace MyUi { class Cursor { } }"),
                CheckFixture.File("Assets/Use.cs", "using UnityEngine;\nCursor.visible = true;"));

            Assert.AreEqual(1, findings.Count, CheckFixture.Dump(findings));
            Assert.AreEqual(Confidence.High, findings[0].Confidence);
        }

        [Test]
        public void ATypeInItsOwnNamespaceShadowsAFileThatImportsIt()
        {
            IReadOnlyList<Finding> findings = CheckFixture.Run(
                CheckFixture.File("Assets/MyCursor.cs", "namespace MyUi { class Cursor { } }"),
                CheckFixture.File("Assets/Use.cs", "using UnityEngine;\nusing MyUi;\nCursor.visible = true;"));

            Assert.AreEqual(1, findings.Count, CheckFixture.Dump(findings));
            Assert.AreEqual(Confidence.Low, findings[0].Confidence);
            Assert.AreEqual("в проекте объявлен свой тип Cursor (MyUi)", findings[0].ConfidenceReason);
        }

        [Test]
        public void ATypeInItsOwnNamespaceShadowsAFileInsideThatNamespace()
        {
            IReadOnlyList<Finding> findings = CheckFixture.Run(
                CheckFixture.File("Assets/MyCursor.cs", "namespace MyUi { class Cursor { } }"),
                CheckFixture.File(
                    "Assets/Use.cs",
                    "using UnityEngine;\nnamespace MyUi.Screens { class S { void M() { Cursor.visible = true; } } }"));

            Assert.AreEqual(1, findings.Count, CheckFixture.Dump(findings));
            Assert.AreEqual(Confidence.Low, findings[0].Confidence);
            Assert.AreEqual("в проекте объявлен свой тип Cursor (MyUi)", findings[0].ConfidenceReason);
        }

        [Test]
        public void AnAliasOntoTheUnityTypeIsCertain()
        {
            Finding finding = CheckFixture.Single("using T = UnityEngine.Time;\nT.timeScale = 0f;");

            Assert.AreEqual("API.TIMESCALE_WRITE", finding.RuleId);
            Assert.AreEqual(Confidence.High, finding.Confidence);
        }

        [Test]
        public void AnAliasOntoASystemIoTypeIsCertainToo()
        {
            Finding finding = CheckFixture.Single("using F = System.IO.File;\nF.WriteAllText(path, text);");

            Assert.AreEqual("API.FILE_WRITE", finding.RuleId);
            Assert.AreEqual(Confidence.High, finding.Confidence);
        }

        [Test]
        public void AnAliasWithoutANamespaceIsOnlyLikely()
        {
            Finding finding = CheckFixture.Single("using T = Time;\nT.timeScale = 0f;");

            Assert.AreEqual(Confidence.Medium, finding.Confidence);
            Assert.AreEqual("алиас без UnityEngine", finding.ConfidenceReason);
        }

        [Test]
        public void AnAliasOntoAForeignTypeTakesTheNameOutOfPlay()
        {
            CheckFixture.None("using Time = MyGame.Time;\nTime.timeScale = 0;");
        }

        [Test]
        public void BareFileWithoutUsingSystemIoIsOnlyAGuess()
        {
            Finding finding = CheckFixture.Single("File.WriteAllText(path, text);");

            Assert.AreEqual("API.FILE_WRITE", finding.RuleId);
            Assert.AreEqual(Confidence.Low, finding.Confidence);
            Assert.AreEqual("File без using System.IO", finding.ConfidenceReason);
        }

        [Test]
        public void BareFileWithUsingSystemIoIsCertain()
        {
            Finding finding = CheckFixture.Single("using System.IO;\nFile.WriteAllText(path, text);");

            Assert.AreEqual(Confidence.High, finding.Confidence);
        }

        [Test]
        public void UsingStaticBringsBareMembersInAtLowConfidence()
        {
            Finding finding = CheckFixture.Single("using static UnityEngine.Time;\nreturn timeScale;");

            Assert.AreEqual("API.TIMESCALE_READ", finding.RuleId);
            Assert.AreEqual(Confidence.Low, finding.Confidence);
            Assert.AreEqual("через using static UnityEngine.Time", finding.ConfidenceReason);
        }

        [Test]
        public void UsingStaticDoesNotTurnDeclarationsIntoFindings()
        {
            CheckFixture.None("using static UnityEngine.Time;\nfloat timeScale = 1f;");
            CheckFixture.None("using static UnityEngine.Time;\nvoid F(float timeScale) { }");
            CheckFixture.None("using static UnityEngine.Application;\nvoid Quit() { }");
        }

        [Test]
        public void UsingStaticFindsABareCall()
        {
            Finding finding = CheckFixture.Single("using static UnityEngine.Application;\nclass C { void M() { Quit(); } }");

            Assert.AreEqual("API.QUIT", finding.RuleId);
            Assert.AreEqual(Confidence.Low, finding.Confidence);
        }

        [Test]
        public void UsingStaticOfPlayerPrefsDoesNotMatchEveryIdentifier()
        {
            CheckFixture.None("using static UnityEngine.PlayerPrefs;\nclass C { void M() { Whatever(); } }");
        }

        [Test]
        public void TheLowestReasonWins()
        {
            IReadOnlyList<Finding> findings = CheckFixture.Run(
                CheckFixture.File("Assets/MyCursor.cs", "class Cursor { }"),
                CheckFixture.File("Assets/Use.cs", "Cursor.visible = true;"));

            Assert.AreEqual(1, findings.Count, CheckFixture.Dump(findings));
            Assert.AreEqual(Confidence.Low, findings[0].Confidence);
            Assert.AreEqual("в проекте объявлен свой тип Cursor (глобальный namespace)", findings[0].ConfidenceReason);
        }
    }
}
