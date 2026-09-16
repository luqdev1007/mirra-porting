using System.Collections.Generic;
using Luqmus.MirraPorting.Code;
using Luqmus.MirraPorting.Core;
using NUnit.Framework;

namespace Luqmus.MirraPorting.Tests.Core
{
    public class DeclaredTypeIndexTests
    {
        [Test]
        public void Build_GroupsNamespacesByName()
        {
            DeclaredTypeIndex index = DeclaredTypeIndex.Build(new[]
            {
                new DeclaredName("Cursor", "MyUi"),
                new DeclaredName("Cursor", string.Empty),
                new DeclaredName("Player", "Game"),
            });

            IReadOnlyList<string> namespaces;
            Assert.IsTrue(index.TryGetNamespaces("Cursor", out namespaces));
            CollectionAssert.AreEquivalent(new[] { "MyUi", string.Empty }, namespaces);
            Assert.AreEqual(2, index.Count);
        }

        [Test]
        public void Build_TheSameDeclarationTwiceIsListedOnce()
        {
            DeclaredTypeIndex index = DeclaredTypeIndex.Build(new[]
            {
                new DeclaredName("Cursor", "MyUi"),
                new DeclaredName("Cursor", "MyUi"),
            });

            IReadOnlyList<string> namespaces;
            Assert.IsTrue(index.TryGetNamespaces("Cursor", out namespaces));
            CollectionAssert.AreEqual(new[] { "MyUi" }, namespaces);
        }

        [Test]
        public void Lookup_IsCaseSensitive()
        {
            DeclaredTypeIndex index = DeclaredTypeIndex.Build(new[] { new DeclaredName("Time", string.Empty) });

            Assert.IsTrue(index.Contains("Time"));
            Assert.IsFalse(index.Contains("time"));

            IReadOnlyList<string> namespaces;
            Assert.IsFalse(index.TryGetNamespaces("time", out namespaces));
            Assert.IsNull(namespaces);
        }

        [Test]
        public void Empty_HasNothing()
        {
            Assert.AreEqual(0, DeclaredTypeIndex.Empty.Count);
            Assert.IsFalse(DeclaredTypeIndex.Empty.Contains("Time"));
        }
    }
}
