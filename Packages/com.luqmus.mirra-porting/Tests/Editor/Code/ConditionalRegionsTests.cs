using Luqmus.MirraPorting.Code;
using NUnit.Framework;

namespace Luqmus.MirraPorting.Tests.Code
{
    public class ConditionalRegionsTests
    {
        [TestCase("UNITY_EDITOR", true)]
        [TestCase("!UNITY_EDITOR", false)]
        [TestCase("!(UNITY_EDITOR)", false)]
        [TestCase("UNITY_EDITOR && DEBUG", true)]
        [TestCase("UNITY_EDITOR || UNITY_WEBGL", false)]
        [TestCase("UNITY_EDITOR_WIN", true)]
        [TestCase("UNITY_EDITOR == false", false)]
        [TestCase("false", true)]
        [TestCase("UNITY_EDITOR // comment", true)]
        public void IsEditorOnlyAt_SingleIfBranch(string condition, bool expected)
        {
            Assert.AreEqual(expected, MarkerIsEditorOnly("#if " + condition + "\nMarker\n#endif"));
        }

        [Test]
        public void IsEditorOnlyAt_ElseOfAnEditorOnlyBranchIsPlayerCode()
        {
            Assert.IsFalse(MarkerIsEditorOnly("#if UNITY_EDITOR\nA\n#else\nMarker\n#endif"));
        }

        [Test]
        public void IsEditorOnlyAt_ElseOfAnUnknownBranchIsUnknownToo()
        {
            Assert.IsFalse(MarkerIsEditorOnly("#if UNITY_WEBGL\nA\n#else\nMarker\n#endif"));
        }

        [Test]
        public void IsEditorOnlyAt_ElifWithAnEditorConditionIsEditorOnly()
        {
            Assert.IsTrue(MarkerIsEditorOnly("#if UNITY_WEBGL\nA\n#elif UNITY_EDITOR\nMarker\n#endif"));
        }

        [Test]
        public void IsEditorOnlyAt_UnknownNestedInsideEditorOnlyIsEditorOnly()
        {
            Assert.IsTrue(MarkerIsEditorOnly("#if UNITY_EDITOR\n#if X\nMarker\n#endif\n#endif"));
        }

        [Test]
        public void IsEditorOnlyAt_EditorOnlyNestedInsideUnknownIsEditorOnly()
        {
            Assert.IsTrue(MarkerIsEditorOnly("#if X\n#if UNITY_EDITOR\nMarker\n#endif\n#endif"));
        }

        [Test]
        public void IsEditorOnlyAt_CodeOutsideAnyBlockIsPlayerCode()
        {
            const string source = "Before\n#if UNITY_EDITOR\nInside\n#endif\nAfter";

            Assert.IsFalse(MarkerIsEditorOnly(source, "Before"));
            Assert.IsTrue(MarkerIsEditorOnly(source, "Inside"));
            Assert.IsFalse(MarkerIsEditorOnly(source, "After"));
        }

        [Test]
        public void IsEditorOnlyAt_RegionAndPragmaDoNotOpenAnything()
        {
            Assert.IsFalse(MarkerIsEditorOnly("#region R\n#pragma warning disable 0649\nMarker\n#endregion"));
        }

        [Test]
        public void IsEditorOnlyAt_RegionInsideAnEditorOnlyBranchChangesNothing()
        {
            Assert.IsTrue(MarkerIsEditorOnly("#if UNITY_EDITOR\n#region R\nMarker\n#endregion\n#endif"));
        }

        [Test]
        public void IsEditorOnlyAt_SpanStartsAfterTheIfLineAndEndsAtTheEndifLine()
        {
            // "#if UNITY_EDITOR" 0..15, newline 16, "Marker" 17..22, newline 23,
            // "#endif" 24..29, newline 30, "After" 31..35.
            const string source = "#if UNITY_EDITOR\nMarker\n#endif\nAfter";
            ConditionalRegions regions = ConditionalRegionBuilder.Build(CSharpLexer.Lex(source));

            Assert.AreEqual(1, regions.EditorOnlySpans.Count);
            Assert.AreEqual(16, regions.EditorOnlySpans[0].Start);
            Assert.AreEqual(24, regions.EditorOnlySpans[0].End);

            Assert.IsTrue(regions.IsEditorOnlyAt(17), "first character after the #if line");
            Assert.IsTrue(regions.IsEditorOnlyAt(22), "last character before the #endif line");
            Assert.IsFalse(regions.IsEditorOnlyAt(24), "first character of the #endif line");
            Assert.IsFalse(regions.IsEditorOnlyAt(31), "after the block");
        }

        [Test]
        public void IsEditorOnlyAt_NestedSpansAreMergedIntoTheParent()
        {
            ConditionalRegions regions = ConditionalRegionBuilder.Build(
                CSharpLexer.Lex("#if UNITY_EDITOR\nA\n#if X\nB\n#endif\nC\n#endif"));

            Assert.AreEqual(1, regions.EditorOnlySpans.Count);
        }

        [Test]
        public void IsEditorOnlyAt_FileWithoutDirectivesHasNoSpans()
        {
            ConditionalRegions regions = ConditionalRegionBuilder.Build(CSharpLexer.Lex("Marker"));

            CollectionAssert.IsEmpty(regions.EditorOnlySpans);
            CollectionAssert.IsEmpty(regions.Diagnostics);
            Assert.IsFalse(regions.IsEditorOnlyAt(0));
        }

        private static bool MarkerIsEditorOnly(string source, string marker = "Marker")
        {
            LexResult lex = CSharpLexer.Lex(source);
            ConditionalRegions regions = ConditionalRegionBuilder.Build(lex);

            foreach (Token token in lex.Tokens)
            {
                if (token.Kind == TokenKind.Identifier && token.Text == marker)
                {
                    return regions.IsEditorOnlyAt(token.Start);
                }
            }

            Assert.Fail("No token '" + marker + "' in:\n" + source);
            return false;
        }
    }
}
