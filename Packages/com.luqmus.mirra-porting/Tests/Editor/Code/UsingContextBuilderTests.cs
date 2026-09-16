using Luqmus.MirraPorting.Code;
using NUnit.Framework;

namespace Luqmus.MirraPorting.Tests.Code
{
    public class UsingContextBuilderTests
    {
        [Test]
        public void Build_StaticUsingAtTheTopLevel()
        {
            UsingContext context = Build("using static UnityEngine.Time;\nclass C { }");

            Assert.AreEqual(1, context.StaticUsings.Count);
            Assert.AreEqual("UnityEngine.Time", context.StaticUsings[0].Dotted);
            Assert.AreEqual("Time", context.StaticUsings[0].Last);
        }

        [Test]
        public void Build_StaticUsingInsideANamespace()
        {
            UsingContext context = Build("namespace N\n{\n    using static UnityEngine.Time;\n}");

            Assert.AreEqual(1, context.StaticUsings.Count);
            Assert.AreEqual("UnityEngine.Time", context.StaticUsings[0].Dotted);
        }

        [Test]
        public void Build_UsingInsideNestedNamespaces()
        {
            UsingContext context = Build("namespace A { namespace B { using static UnityEngine.Time; } }");

            Assert.AreEqual(1, context.StaticUsings.Count);
        }

        [Test]
        public void Build_Alias()
        {
            UsingContext context = Build("using T = UnityEngine.Time;");

            Assert.AreEqual(1, context.Aliases.Count);
            Assert.AreEqual("UnityEngine.Time", context.Aliases["T"].Dotted);
            Assert.IsFalse(context.Aliases["T"].HadGlobalQualifier);
        }

        [Test]
        public void Build_AliasWithGlobalQualifier()
        {
            UsingContext context = Build("using T = global::UnityEngine.Time;");

            Assert.AreEqual("UnityEngine.Time", context.Aliases["T"].Dotted);
            Assert.IsTrue(context.Aliases["T"].HadGlobalQualifier);
        }

        [Test]
        public void Build_SameAliasInTwoBranchesKeepsTheLastOne()
        {
            UsingContext context = Build(
                "#if UNITY_EDITOR\nusing T = UnityEngine.Time;\n#else\nusing T = UnityEngine.Screen;\n#endif");

            Assert.AreEqual(1, context.Aliases.Count);
            Assert.AreEqual("UnityEngine.Screen", context.Aliases["T"].Dotted);
        }

        [Test]
        public void Build_AliasOfAGenericTypeKeepsThePartsBeforeTheTypeArguments()
        {
            UsingContext context = Build("using L = System.Collections.Generic.List<int>;");

            Assert.AreEqual("System.Collections.Generic.List", context.Aliases["L"].Dotted);
        }

        [Test]
        public void Build_PlainNamespaceUsing()
        {
            UsingContext context = Build("using System.IO;\nusing UnityEngine;");

            Assert.AreEqual(2, context.Namespaces.Count);
            Assert.IsTrue(context.UsesNamespace("System.IO"));
            Assert.IsTrue(context.UsesNamespace("UnityEngine"));
            Assert.IsFalse(context.UsesNamespace("System"));
        }

        [Test]
        public void Build_UsingStatementsInsideAMethodAreNotDirectives()
        {
            UsingContext context = Build(
                "class C\n{\n    void M()\n    {\n        using (var s = x) { }\n        using var y = z;\n    }\n}");

            CollectionAssert.IsEmpty(context.Namespaces);
            CollectionAssert.IsEmpty(context.StaticUsings);
            CollectionAssert.IsEmpty(context.Aliases);
        }

        [Test]
        public void Build_UsingInsideATypeBodyIsNotADirective()
        {
            UsingContext context = Build("class C { using System.IO; }");

            CollectionAssert.IsEmpty(context.Namespaces);
        }

        [Test]
        public void Build_FileWithoutUsings()
        {
            UsingContext context = Build("class C { }");

            CollectionAssert.IsEmpty(context.Namespaces);
            CollectionAssert.IsEmpty(context.StaticUsings);
            CollectionAssert.IsEmpty(context.Aliases);
        }

        private static UsingContext Build(string source)
        {
            return UsingContextBuilder.Build(CSharpLexer.Lex(source).Tokens);
        }
    }
}
