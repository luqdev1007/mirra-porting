using System.Collections.Generic;
using System.Linq;
using Luqmus.MirraPorting.Code;
using Luqmus.MirraPorting.Core;
using NUnit.Framework;

namespace Luqmus.MirraPorting.Tests.Core
{
    public class SourceFileTests
    {
        [Test]
        public void FromText_WiresUpEveryLayer()
        {
            SourceFile file = Parse("using static UnityEngine.Time;\nnamespace Game { class Time { } }");

            Assert.Greater(file.Lex.Tokens.Count, 0);
            Assert.AreEqual(1, file.Usings.StaticUsings.Count);
            CollectionAssert.AreEqual(new[] { "Game", "Time" }, Names(file));
            Assert.AreEqual("Assets/A.cs", file.ProjectRelativePath);
        }

        [Test]
        public void IsEditorOnlyAt_UsesTheConditionalRegions()
        {
            const string source = "Outside\n#if UNITY_EDITOR\nInside\n#endif";
            SourceFile file = Parse(source);

            Assert.IsFalse(file.EditorOnly);
            Assert.IsFalse(file.IsEditorOnlyAt(OffsetOf(file, "Outside")));
            Assert.IsTrue(file.IsEditorOnlyAt(OffsetOf(file, "Inside")));
        }

        [Test]
        public void IsEditorOnlyAt_AnEditorOnlyFileIsEditorOnlyEverywhere()
        {
            const string source = "Outside\n#if UNITY_EDITOR\nInside\n#endif";
            SourceFile file = Parse(source, editorOnly: true);

            Assert.IsTrue(file.EditorOnly);
            Assert.IsTrue(file.IsEditorOnlyAt(OffsetOf(file, "Outside")));
            Assert.IsTrue(file.IsEditorOnlyAt(OffsetOf(file, "Inside")));
        }

        [Test]
        public void DiagnosticCount_AddsUpLexerAndDirectiveDiagnostics()
        {
            SourceFile clean = Parse("class C { }");
            Assert.AreEqual(0, clean.DiagnosticCount);

            SourceFile broken = Parse("var s = \"abc\n#endif\ndon't");
            Assert.AreEqual(3, broken.DiagnosticCount);
        }

        [Test]
        public void FirstTruncatingDiagnostic_NamesWhatSwallowedTheRestOfTheFile()
        {
            Assert.IsNull(Parse("class C { }").FirstTruncatingDiagnostic);

            // Recovered on the line it started on: nothing below was lost.
            Assert.IsNull(Parse("var s = \"abc\nclass C { }").FirstTruncatingDiagnostic);

            AssertTruncatedAt(Parse("class C { }\n/* tail"), LexDiagnosticCode.UnterminatedComment, 2);
            AssertTruncatedAt(Parse("class C { }\n@\"tail"), LexDiagnosticCode.UnterminatedVerbatimString, 2);
            AssertTruncatedAt(Parse("class C { }\n$@\"tail"), LexDiagnosticCode.UnterminatedVerbatimString, 2);
            AssertTruncatedAt(Parse("class C { }\n#if X\nclass D { }"), LexDiagnosticCode.UnclosedConditional, 2);
        }

        [Test]
        public void InternalErrorDiagnostic_IsAbsentOnOrdinaryFiles()
        {
            Assert.IsNull(Parse("class C { }").InternalErrorDiagnostic);
            Assert.IsNull(Parse("$\"{").InternalErrorDiagnostic);
        }

        private static void AssertTruncatedAt(SourceFile file, LexDiagnosticCode expected, int line)
        {
            Assert.IsNotNull(file.FirstTruncatingDiagnostic, "Expected " + expected);
            Assert.AreEqual(expected, file.FirstTruncatingDiagnostic.Code);
            Assert.AreEqual(line, file.FirstTruncatingDiagnostic.Line);
        }

        private static IReadOnlyList<string> Names(SourceFile file)
        {
            return file.Declarations.Names.Select(declared => declared.Name).ToList();
        }

        private static int OffsetOf(SourceFile file, string tokenText)
        {
            foreach (Token token in file.Lex.Tokens)
            {
                if (token.Kind == TokenKind.Identifier && token.Text == tokenText)
                {
                    return token.Start;
                }
            }

            Assert.Fail("No token '" + tokenText + "'");
            return 0;
        }

        private static SourceFile Parse(string source, bool editorOnly = false)
        {
            var scoped = new ScopedFile("Assets/A.cs", "D:/project/Assets/A.cs", editorOnly);
            return SourceFile.FromText(scoped, source);
        }
    }
}
