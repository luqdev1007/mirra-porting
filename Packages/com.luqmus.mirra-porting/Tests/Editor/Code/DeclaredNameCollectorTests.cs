using System.Collections.Generic;
using System.Linq;
using Luqmus.MirraPorting.Code;
using NUnit.Framework;

namespace Luqmus.MirraPorting.Tests.Code
{
    public class DeclaredNameCollectorTests
    {
        [Test]
        public void Collect_TypeDeclarationsThatShadowUnityTypes()
        {
            CollectionAssert.AreEqual(new[] { "Time" }, Names("class Time { }"));
            CollectionAssert.AreEqual(new[] { "Cursor" }, Names("struct Cursor { }"));
            CollectionAssert.AreEqual(new[] { "Screen" }, Names("enum Screen { Wide }"));
            CollectionAssert.AreEqual(new[] { "IFoo" }, Names("interface IFoo { }"));
        }

        [Test]
        public void Collect_NestedType()
        {
            CollectionAssert.AreEqual(
                new[] { "Outer", "Application" },
                Names("class Outer { class Application { } }"));
        }

        [Test]
        public void Collect_GenericConstraintsAreNotDeclarations()
        {
            CollectionAssert.AreEqual(new[] { "C" }, Names("class C<T> where T : class, IFoo { }"));
            CollectionAssert.AreEqual(new[] { "D" }, Names("class D<T, U> where T : struct where U : new() { }"));
        }

        [Test]
        public void Collect_NamesAreCaseSensitive()
        {
            CollectionAssert.AreEqual(new[] { "Time", "time" }, Names("class Time { }\nclass time { }"));
        }

        [Test]
        public void Collect_TheSameNameTwiceIsListedOnce()
        {
            CollectionAssert.AreEqual(
                new[] { "Time" },
                Names("#if UNITY_EDITOR\nclass Time { }\n#else\nclass Time { }\n#endif"));
        }

        [Test]
        public void Collect_TypeInAGlobalNamespace()
        {
            CollectionAssert.AreEqual(new[] { "|Cursor" }, Qualified("class Cursor { }"));
        }

        [Test]
        public void Collect_TypeInsideANamespaceCarriesIt()
        {
            CollectionAssert.AreEqual(
                new[] { "|MyUi", "MyUi|Cursor" },
                Qualified("namespace MyUi { class Cursor { } }"));
        }

        [Test]
        public void Collect_NestedTypeKeepsTheNamespaceOfTheOuterType()
        {
            CollectionAssert.AreEqual(
                new[] { "|MyUi", "MyUi|Outer", "MyUi|Cursor" },
                Qualified("namespace MyUi { class Outer { class Cursor { } } }"));
        }

        [Test]
        public void Collect_NamespaceSegmentsEachSitInTheirParent()
        {
            CollectionAssert.AreEqual(
                new[] { "|Game", "Game|Time", "Game.Time|Player" },
                Qualified("namespace Game.Time { class Player { } }"));
        }

        [Test]
        public void Collect_NestedNamespacesAreJoined()
        {
            DeclaredNames declarations = Collect("namespace A { namespace B.C { class D { } } }");

            CollectionAssert.AreEqual(new[] { "A", "A.B.C" }, declarations.Namespaces);
            CollectionAssert.AreEqual(
                new[] { "|A", "A|B", "A.B|C", "A.B.C|D" },
                declarations.Names.Select(Key).ToList());
        }

        [Test]
        public void Collect_TypeAfterAClosedNamespaceIsGlobalAgain()
        {
            CollectionAssert.AreEqual(
                new[] { "|MyUi", "MyUi|Cursor", "|Time" },
                Qualified("namespace MyUi { class Cursor { } }\nclass Time { }"));
        }

        [Test]
        public void Collect_NamespacesOfAFileWithoutAny()
        {
            CollectionAssert.IsEmpty(Collect("class C { }").Namespaces);
        }

        [Test]
        public void Collect_NothingToDeclare()
        {
            CollectionAssert.IsEmpty(Names("var x = 1;"));
            CollectionAssert.IsEmpty(Names(string.Empty));
        }

        private static DeclaredNames Collect(string source)
        {
            return DeclaredNameCollector.Collect(CSharpLexer.Lex(source).Tokens);
        }

        private static IReadOnlyList<string> Names(string source)
        {
            return Collect(source).Names.Select(declared => declared.Name).ToList();
        }

        /// <summary>"namespace|Name", with an empty namespace for the global one.</summary>
        private static IReadOnlyList<string> Qualified(string source)
        {
            return Collect(source).Names.Select(Key).ToList();
        }

        private static string Key(DeclaredName declared)
        {
            return declared.Namespace + "|" + declared.Name;
        }
    }
}
