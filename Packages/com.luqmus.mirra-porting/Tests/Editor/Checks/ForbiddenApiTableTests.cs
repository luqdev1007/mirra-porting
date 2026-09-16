using System.Collections.Generic;
using Luqmus.MirraPorting.Checks;
using Luqmus.MirraPorting.Core;
using NUnit.Framework;

namespace Luqmus.MirraPorting.Tests.Checks
{
    public class ForbiddenApiTableTests
    {
        [Test]
        public void EveryRuleIdInTheTableExistsInTheCatalog()
        {
            foreach (ForbiddenMember member in ForbiddenApiTable.All)
            {
                AssertKnownRule(member.WriteRuleId, member);
                AssertKnownRule(member.ReadRuleId, member);
                AssertKnownRule(member.CallRuleId, member);
            }
        }

        [Test]
        public void EveryRowHasAtLeastOneRule()
        {
            foreach (ForbiddenMember member in ForbiddenApiTable.All)
            {
                bool any = member.WriteRuleId != null || member.ReadRuleId != null || member.CallRuleId != null;
                Assert.IsTrue(any, "Row without a rule: " + member);
            }
        }

        [Test]
        public void PropertiesAreNeverCalls()
        {
            foreach (ForbiddenMember member in ForbiddenApiTable.All)
            {
                if (member.Kind == ApiMemberKind.Property)
                {
                    Assert.IsNull(member.CallRuleId, "Property with a call rule: " + member);
                }
            }
        }

        [Test]
        public void MethodsReportAMethodGroupTheSameWayAsACall()
        {
            foreach (ForbiddenMember member in ForbiddenApiTable.All)
            {
                if (member.Kind == ApiMemberKind.Method)
                {
                    Assert.AreEqual(member.CallRuleId, member.ReadRuleId, "Method: " + member);
                    Assert.IsNull(member.WriteRuleId, "Method with a write rule: " + member);
                }
            }
        }

        [Test]
        public void TryFind_ExactMember()
        {
            ForbiddenMember member;

            Assert.IsTrue(ForbiddenApiTable.TryFind("Time", "timeScale", out member));
            Assert.AreEqual("API.TIMESCALE_WRITE", member.RuleIdFor(AccessKind.Write));
            Assert.AreEqual("API.TIMESCALE_READ", member.RuleIdFor(AccessKind.Read));
            Assert.IsNull(member.RuleIdFor(AccessKind.Call));
        }

        [Test]
        public void TryFind_AnyMemberOfPlayerPrefs()
        {
            ForbiddenMember member;

            Assert.IsTrue(ForbiddenApiTable.TryFind("PlayerPrefs", "GetInt", out member));
            Assert.IsTrue(member.MatchesAnyMember);
            Assert.AreEqual("API.PLAYERPREFS", member.RuleIdFor(AccessKind.Call));
        }

        [Test]
        public void TryFind_MemberThatIsNotInTheTable()
        {
            ForbiddenMember member;

            Assert.IsFalse(ForbiddenApiTable.TryFind("Time", "deltaTime", out member));
            Assert.IsFalse(ForbiddenApiTable.TryFind("Debug", "Log", out member));
            Assert.IsFalse(ForbiddenApiTable.TryFind(null, "timeScale", out member));
        }

        [Test]
        public void ReadOnlyAndWriteOnlyMembers()
        {
            ForbiddenMember orientation;
            Assert.IsTrue(ForbiddenApiTable.TryFind("Screen", "orientation", out orientation));
            Assert.AreEqual("API.ORIENTATION_WRITE", orientation.RuleIdFor(AccessKind.Write));
            Assert.IsNull(orientation.RuleIdFor(AccessKind.Read));

            ForbiddenMember language;
            Assert.IsTrue(ForbiddenApiTable.TryFind("Application", "systemLanguage", out language));
            Assert.AreEqual("API.SYSTEM_LANGUAGE", language.RuleIdFor(AccessKind.Read));
            Assert.IsNull(language.RuleIdFor(AccessKind.Write));
        }

        [Test]
        public void NamespaceOf_TellsUnityTypesFromSystemIoOnes()
        {
            Assert.AreEqual("UnityEngine", ForbiddenApiTable.NamespaceOf("Time"));
            Assert.AreEqual("UnityEngine", ForbiddenApiTable.NamespaceOf("Screen"));
            Assert.AreEqual("System.IO", ForbiddenApiTable.NamespaceOf("File"));
            Assert.AreEqual(string.Empty, ForbiddenApiTable.NamespaceOf("Nope"));
        }

        [Test]
        public void TypeNames_CoverTheWholeTable()
        {
            CollectionAssert.AreEquivalent(
                new[] { "Time", "AudioListener", "Cursor", "PlayerPrefs", "Application", "SystemInfo", "Screen", "File" },
                ForbiddenApiTable.TypeNames);
            Assert.IsTrue(ForbiddenApiTable.IsKnownType("Cursor"));
            Assert.IsFalse(ForbiddenApiTable.IsKnownType("cursor"));
        }

        [Test]
        public void TryGetMembers_ReturnsEveryRowOfAType()
        {
            IReadOnlyList<ForbiddenMember> members;

            Assert.IsTrue(ForbiddenApiTable.TryGetMembers("Cursor", out members));
            Assert.AreEqual(2, members.Count);
            Assert.IsFalse(ForbiddenApiTable.TryGetMembers("Nope", out members));
        }

        private static void AssertKnownRule(string ruleId, ForbiddenMember member)
        {
            if (ruleId == null)
            {
                return;
            }

            Assert.IsNotNull(Rules.Find(ruleId), "Rule " + ruleId + " of " + member + " is missing from the catalog");
        }
    }
}
