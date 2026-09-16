using System.Collections.Generic;
using Luqmus.MirraPorting.Code;
using NUnit.Framework;

namespace Luqmus.MirraPorting.Tests.Code
{
    public class DeclaredNameCollectorTests
    {
        [Test]
        public void Collect_TypeDeclarationsThatShadowUnityTypes()
        {
            CollectionAssert.AreEqual(new[] { "Time" }, Collect("class Time { }"));
            CollectionAssert.AreEqual(new[] { "Cursor" }, Collect("struct Cursor { }"));
            CollectionAssert.AreEqual(new[] { "Screen" }, Collect("enum Screen { Wide }"));
            CollectionAssert.AreEqual(new[] { "IFoo" }, Collect("interface IFoo { }"));
        }

        [Test]
        public void Collect_NestedType()
        {
            CollectionAssert.AreEqual(
                new[] { "Outer", "Application" },
                Collect("class Outer { class Application { } }"));
        }

        [Test]
        public void Collect_GenericConstraintsAreNotDeclarations()
        {
            CollectionAssert.AreEqual(new[] { "C" }, Collect("class C<T> where T : class, IFoo { }"));
            CollectionAssert.AreEqual(new[] { "D" }, Collect("class D<T, U> where T : struct where U : new() { }"));
        }

        [Test]
        public void Collect_NamespaceSegments()
        {
            CollectionAssert.AreEqual(
                new[] { "Game", "Time", "Player" },
                Collect("namespace Game.Time { class Player { } }"));
        }

        [Test]
        public void Collect_NamesAreCaseSensitive()
        {
            CollectionAssert.AreEqual(new[] { "Time", "time" }, Collect("class Time { }\nclass time { }"));
        }

        [Test]
        public void Collect_TheSameNameTwiceIsListedOnce()
        {
            CollectionAssert.AreEqual(
                new[] { "Time" },
                Collect("#if UNITY_EDITOR\nclass Time { }\n#else\nclass Time { }\n#endif"));
        }

        [Test]
        public void Collect_NothingToDeclare()
        {
            CollectionAssert.IsEmpty(Collect("var x = 1;"));
            CollectionAssert.IsEmpty(Collect(string.Empty));
        }

        private static IReadOnlyList<string> Collect(string source)
        {
            return DeclaredNameCollector.Collect(CSharpLexer.Lex(source).Tokens);
        }
    }
}
