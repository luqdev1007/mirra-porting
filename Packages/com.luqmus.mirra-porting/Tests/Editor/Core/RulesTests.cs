using System.Collections.Generic;
using Luqmus.MirraPorting.Core;
using NUnit.Framework;

namespace Luqmus.MirraPorting.Tests.Core
{
    public class RulesTests
    {
        [Test]
        public void All_RuleIdsAreUnique()
        {
            var seen = new HashSet<string>();
            foreach (Rule rule in Rules.All)
            {
                Assert.IsTrue(seen.Add(rule.Id), "Duplicate rule id: " + rule.Id);
            }
        }

        [Test]
        public void All_HaveIdCategoryAndMessage()
        {
            foreach (Rule rule in Rules.All)
            {
                Assert.IsNotEmpty(rule.Id);
                Assert.IsNotEmpty(rule.Category);
                Assert.IsNotEmpty(rule.Message, "Rule without a message: " + rule.Id);
                Assert.IsNotNull(rule.Suggestion, "Rule with a null suggestion: " + rule.Id);
            }
        }

        [Test]
        public void Find_ReturnsRuleById()
        {
            Assert.AreSame(Rules.ScanInternalError, Rules.Find("SCAN.INTERNAL_ERROR"));
        }

        [Test]
        public void Find_UnknownIdReturnsNull()
        {
            Assert.IsNull(Rules.Find("API.NOPE"));
            Assert.IsNull(Rules.Find(null));
        }

        [Test]
        public void ForbiddenApiRules_AreAllInTheForbiddenApiCategory()
        {
            foreach (Rule rule in Rules.All)
            {
                if (rule.Id.StartsWith("API.", System.StringComparison.Ordinal))
                {
                    Assert.AreEqual(Categories.ForbiddenApi, rule.Category, rule.Id);
                }
            }
        }

        [Test]
        public void WriteRules_AreErrorsAndReadRulesAreWarnings()
        {
            Assert.AreEqual(Severity.Error, Rules.TimeScaleWrite.Severity);
            Assert.AreEqual(Severity.Warning, Rules.TimeScaleRead.Severity);
            Assert.AreEqual(Severity.Error, Rules.CursorLockWrite.Severity);
            Assert.AreEqual(Severity.Warning, Rules.CursorLockRead.Severity);
            Assert.AreEqual(Severity.Warning, Rules.OrientationWrite.Severity);
            Assert.AreEqual(Severity.Error, Rules.PlayerPrefs.Severity);
        }

        [Test]
        public void ScanInternalError_IsAWarningInTheScanCategory()
        {
            Assert.AreEqual(Severity.Warning, Rules.ScanInternalError.Severity);
            Assert.AreEqual(Categories.Scan, Rules.ScanInternalError.Category);
        }
    }
}
