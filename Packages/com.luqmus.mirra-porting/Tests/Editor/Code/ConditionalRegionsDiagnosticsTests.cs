using System.Linq;
using Luqmus.MirraPorting.Code;
using NUnit.Framework;

namespace Luqmus.MirraPorting.Tests.Code
{
    public class ConditionalRegionsDiagnosticsTests
    {
        [TestCase("#endif")]
        [TestCase("#else")]
        [TestCase("#elif X")]
        public void Build_DirectiveWithoutAnIfIsReported(string source)
        {
            ConditionalRegions regions = Build(source);

            CollectionAssert.AreEqual(
                new[] { LexDiagnosticCode.UnbalancedDirective },
                regions.Diagnostics.Select(d => d.Code).ToList());
        }

        [Test]
        public void Build_SecondElseIsReportedAndTheBlockKeepsGoing()
        {
            ConditionalRegions regions = Build("#if UNITY_EDITOR\nA\n#else\nB\n#else\nC\n#endif");

            CollectionAssert.AreEqual(
                new[] { LexDiagnosticCode.UnbalancedDirective },
                regions.Diagnostics.Select(d => d.Code).ToList());
        }

        [Test]
        public void Build_UnclosedIfIsReportedAndItsBranchRunsToTheEndOfTheFile()
        {
            const string source = "#if UNITY_EDITOR\nMarker";
            LexResult lex = CSharpLexer.Lex(source);
            ConditionalRegions regions = ConditionalRegionBuilder.Build(lex);

            CollectionAssert.AreEqual(
                new[] { LexDiagnosticCode.UnclosedConditional },
                regions.Diagnostics.Select(d => d.Code).ToList());
            Assert.AreEqual(1, regions.Diagnostics[0].Line);
            Assert.AreEqual(1, regions.Diagnostics[0].Column);
            Assert.AreEqual(1, regions.EditorOnlySpans.Count);
            Assert.AreEqual(source.Length, regions.EditorOnlySpans[0].End);
            Assert.IsTrue(regions.IsEditorOnlyAt(17));
        }

        [Test]
        public void Build_BrokenConditionIsReportedAndItsBranchIsNotEditorOnly()
        {
            const string source = "#if (UNITY_EDITOR &&\nMarker\n#endif";
            LexResult lex = CSharpLexer.Lex(source);
            ConditionalRegions regions = ConditionalRegionBuilder.Build(lex);

            CollectionAssert.AreEqual(
                new[] { LexDiagnosticCode.InvalidConditionExpression },
                regions.Diagnostics.Select(d => d.Code).ToList());
            CollectionAssert.IsEmpty(regions.EditorOnlySpans);
        }

        [TestCase("#if")]
        [TestCase("#")]
        [TestCase("#endif\n#endif")]
        [TestCase("#if X\n#else\n#elif Y\n#endif")]
        [TestCase("#if UNITY_EDITOR\n#if X\n#endif")]
        [TestCase("#else\n#endif\n#if X")]
        public void Build_NeverThrowsOnBrokenDirectives(string source)
        {
            ConditionalRegions regions = Build(source);

            Assert.IsNotNull(regions);
            Assert.IsNotNull(regions.EditorOnlySpans);
            Assert.IsNotNull(regions.Diagnostics);
            CollectionAssert.DoesNotContain(
                regions.Diagnostics.Select(d => d.Code).ToList(),
                LexDiagnosticCode.InternalError);
        }

        private static ConditionalRegions Build(string source)
        {
            return ConditionalRegionBuilder.Build(CSharpLexer.Lex(source));
        }
    }
}
