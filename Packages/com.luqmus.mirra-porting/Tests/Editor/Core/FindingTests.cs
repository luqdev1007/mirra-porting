using System;
using Luqmus.MirraPorting.Core;
using NUnit.Framework;

namespace Luqmus.MirraPorting.Tests.Core
{
    public class FindingTests
    {
        private static readonly Rule ErrorRule =
            new Rule("API.TEST_WRITE", Categories.ForbiddenApi, Severity.Error, "message", "suggestion");

        private static readonly Rule InfoRule =
            new Rule("API.TEST_INFO", Categories.ForbiddenApi, Severity.Info, "message", "suggestion");

        [Test]
        public void FromRule_CopiesRuleTextAndPosition()
        {
            Finding finding = Finding.FromRule(
                ErrorRule, "Assets/Scripts/PauseMenu.cs", 42, 9, "Time.timeScale = 0f;", Confidence.High, string.Empty, false);

            Assert.AreEqual("API.TEST_WRITE", finding.RuleId);
            Assert.AreEqual(Categories.ForbiddenApi, finding.Category);
            Assert.AreEqual(Severity.Error, finding.Severity);
            Assert.AreEqual(Confidence.High, finding.Confidence);
            Assert.IsFalse(finding.EditorOnly);
            Assert.AreEqual("Assets/Scripts/PauseMenu.cs", finding.Path);
            Assert.AreEqual(42, finding.Line);
            Assert.AreEqual(9, finding.Column);
            Assert.AreEqual("Time.timeScale = 0f;", finding.Snippet);
            Assert.AreEqual("message", finding.Message);
            Assert.AreEqual("suggestion", finding.Suggestion);
        }

        [Test]
        public void FromRule_EditorOnlyLowersSeverityToInfo()
        {
            Finding finding = Finding.FromRule(
                ErrorRule, "Assets/Editor/Tool.cs", 1, 1, "Time.timeScale = 0f;", Confidence.High, string.Empty, true);

            Assert.AreEqual(Severity.Info, finding.Severity);
            Assert.IsTrue(finding.EditorOnly);
        }

        [Test]
        public void FromRule_EditorOnlyKeepsSeverityOfAnInfoRule()
        {
            Finding finding = Finding.FromRule(
                InfoRule, "Assets/Editor/Tool.cs", 1, 1, "snippet", Confidence.High, string.Empty, true);

            Assert.AreEqual(Severity.Info, finding.Severity);
        }

        [Test]
        public void FromRule_TrimsTheSnippet()
        {
            Finding finding = Finding.FromRule(
                ErrorRule, "Assets/A.cs", 1, 1, "    Time.timeScale = 0f;\t", Confidence.High, string.Empty, false);

            Assert.AreEqual("Time.timeScale = 0f;", finding.Snippet);
        }

        [Test]
        public void FromRule_CutsALongSnippet()
        {
            string longLine = new string('x', 400);

            Finding finding = Finding.FromRule(
                ErrorRule, "Assets/A.cs", 1, 1, longLine, Confidence.High, string.Empty, false);

            Assert.AreEqual(Finding.MaxSnippetLength, finding.Snippet.Length);
            Assert.IsTrue(finding.Snippet.EndsWith("…", StringComparison.Ordinal));
        }

        [Test]
        public void FromRule_NullSnippetBecomesEmpty()
        {
            Finding finding = Finding.FromRule(ErrorRule, "Assets/A.cs", 1, 1, null, Confidence.High, string.Empty, false);

            Assert.AreEqual(string.Empty, finding.Snippet);
        }

        [Test]
        public void FromRule_MessageOverrideReplacesTheRuleMessage()
        {
            Finding finding = Finding.FromRule(
                Rules.ScanInternalError, "Assets/A.cs", 0, 0, string.Empty, Confidence.High, string.Empty, false,
                "NullReferenceException в TestCheck");

            Assert.AreEqual("NullReferenceException в TestCheck", finding.Message);
            Assert.AreEqual(Rules.ScanInternalError.Suggestion, finding.Suggestion);
        }

        [Test]
        public void FromRule_ConfidenceReasonIsKeptWhenTheConfidenceIsNotHigh()
        {
            Finding finding = Finding.FromRule(
                ErrorRule, "Assets/A.cs", 1, 1, "x", Confidence.Low, "в проекте объявлен свой тип Cursor (MyUi)", false);

            Assert.AreEqual("в проекте объявлен свой тип Cursor (MyUi)", finding.ConfidenceReason);
        }

        [Test]
        public void FromRule_HighConfidenceNeverCarriesAReason()
        {
            Finding finding = Finding.FromRule(
                ErrorRule, "Assets/A.cs", 1, 1, "x", Confidence.High, "что-то", false);

            Assert.AreEqual(string.Empty, finding.ConfidenceReason);
        }

        [Test]
        public void FromRule_MissingReasonBecomesEmpty()
        {
            Finding finding = Finding.FromRule(ErrorRule, "Assets/A.cs", 1, 1, "x", Confidence.Low, null, false);

            Assert.AreEqual(string.Empty, finding.ConfidenceReason);
        }

        [Test]
        public void FromRule_NullRuleThrows()
        {
            Assert.Throws<ArgumentNullException>(
                () => Finding.FromRule(null, "Assets/A.cs", 1, 1, "x", Confidence.High, string.Empty, false));
        }
    }
}
