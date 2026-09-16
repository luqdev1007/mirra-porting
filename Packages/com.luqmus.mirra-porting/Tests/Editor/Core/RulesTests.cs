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
        public void ScanInternalError_IsAWarningInTheScanCategory()
        {
            Assert.AreEqual(Severity.Warning, Rules.ScanInternalError.Severity);
            Assert.AreEqual(Categories.Scan, Rules.ScanInternalError.Category);
        }
    }
}
