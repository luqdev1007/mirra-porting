using System;
using System.Collections.Generic;
using System.Reflection;
using Luqmus.MirraPorting.Checks;
using NUnit.Framework;

namespace Luqmus.MirraPorting.Tests.Checks
{
    /// <summary>
    /// Checks the table against the API it describes. Member names are matched case sensitively by
    /// the scanner, so a typo makes a rule silently match nothing — Screen.autorotateToPortrait is
    /// spelled with a lower case r, and a table that said otherwise found no orientation locks at
    /// all.
    /// </summary>
    public class ForbiddenApiTableReflectionTests
    {
        private static readonly Dictionary<string, Type> RealTypes = new Dictionary<string, Type>(StringComparer.Ordinal)
        {
            { "Time", typeof(UnityEngine.Time) },
            { "AudioListener", typeof(UnityEngine.AudioListener) },
            { "Cursor", typeof(UnityEngine.Cursor) },
            { "PlayerPrefs", typeof(UnityEngine.PlayerPrefs) },
            { "Application", typeof(UnityEngine.Application) },
            { "SystemInfo", typeof(UnityEngine.SystemInfo) },
            { "Screen", typeof(UnityEngine.Screen) },
            { "File", typeof(System.IO.File) },
        };

        [Test]
        public void EveryTypeOfTheTableIsCovered()
        {
            CollectionAssert.AreEquivalent(RealTypes.Keys, ForbiddenApiTable.TypeNames);
        }

        [Test]
        public void EveryTypeSitsInTheNamespaceTheTableClaims()
        {
            foreach (ForbiddenMember member in ForbiddenApiTable.All)
            {
                Assert.AreEqual(member.Namespace, RealTypes[member.TypeName].Namespace, member.TypeName);
            }
        }

        [Test]
        public void EveryMemberOfTheTableExistsWithThatExactName()
        {
            foreach (ForbiddenMember member in ForbiddenApiTable.All)
            {
                if (member.MatchesAnyMember)
                {
                    continue;
                }

                Type type = RealTypes[member.TypeName];
                MemberInfo[] found = type.GetMember(
                    member.MemberName, BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);

                Assert.Greater(found.Length, 0, "No public static " + member + " in " + type.FullName);
            }
        }

        [Test]
        public void EveryMemberIsOfTheKindTheTableClaims()
        {
            foreach (ForbiddenMember member in ForbiddenApiTable.All)
            {
                if (member.MatchesAnyMember)
                {
                    continue;
                }

                Type type = RealTypes[member.TypeName];
                BindingFlags flags = BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy;

                if (member.Kind == ApiMemberKind.Property)
                {
                    Assert.IsNotNull(type.GetProperty(member.MemberName, flags), member + " is not a property");
                }
                else
                {
                    Assert.Greater(
                        type.GetMember(member.MemberName, MemberTypes.Method, flags).Length, 0,
                        member + " is not a method");
                }
            }
        }

        [Test]
        public void WriteOnlyMembersCanReallyBeWritten()
        {
            foreach (ForbiddenMember member in ForbiddenApiTable.All)
            {
                if (member.MatchesAnyMember || member.Kind != ApiMemberKind.Property || member.WriteRuleId == null)
                {
                    continue;
                }

                PropertyInfo property = RealTypes[member.TypeName].GetProperty(
                    member.MemberName, BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);

                Assert.IsTrue(property.CanWrite, member + " has a write rule but is read only");
            }
        }

        [Test]
        public void ReadOnlyRulesAreNotOnWriteOnlyMembers()
        {
            foreach (ForbiddenMember member in ForbiddenApiTable.All)
            {
                if (member.MatchesAnyMember || member.Kind != ApiMemberKind.Property || member.ReadRuleId == null)
                {
                    continue;
                }

                PropertyInfo property = RealTypes[member.TypeName].GetProperty(
                    member.MemberName, BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);

                Assert.IsTrue(property.CanRead, member + " has a read rule but cannot be read");
            }
        }
    }
}
