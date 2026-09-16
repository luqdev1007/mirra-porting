using System.Collections.Generic;
using Luqmus.MirraPorting.Code;

namespace Luqmus.MirraPorting.Core
{
    /// <summary>
    /// A scanned file after it has been read: its tokens, the branches the player build drops, its
    /// using directives and the names it declares.
    /// </summary>
    internal sealed class SourceFile
    {
        /// <summary>
        /// Diagnostics after which the file was no longer read as code: whatever follows them was
        /// swallowed by a comment, a verbatim string or a conditional that never closed. The
        /// report names these files, because findings below such a line are lost silently.
        /// </summary>
        private static readonly LexDiagnosticCode[] TruncatingCodes =
        {
            LexDiagnosticCode.UnterminatedComment,
            LexDiagnosticCode.UnterminatedVerbatimString,
            LexDiagnosticCode.UnclosedConditional,
        };

        private SourceFile(
            ScopedFile file,
            LexResult lex,
            ConditionalRegions regions,
            UsingContext usings,
            IReadOnlyList<string> declaredNames)
        {
            File = file;
            Lex = lex;
            Regions = regions;
            Usings = usings;
            DeclaredNames = declaredNames;

            DiagnosticCount = CountReportable(lex.Diagnostics) + CountReportable(regions.Diagnostics);
            InternalErrorDiagnostic = FirstOf(lex, regions, LexDiagnosticCode.InternalError);
            FirstTruncatingDiagnostic = FirstOf(lex, regions, TruncatingCodes);
        }

        internal ScopedFile File { get; }

        internal LexResult Lex { get; }

        internal ConditionalRegions Regions { get; }

        internal UsingContext Usings { get; }

        /// <summary>Type and namespace names this file declares.</summary>
        internal IReadOnlyList<string> DeclaredNames { get; }

        internal string ProjectRelativePath
        {
            get { return File.ProjectRelativePath; }
        }

        /// <summary>The whole file is editor only, by assembly or by folder.</summary>
        internal bool EditorOnly
        {
            get { return File.EditorOnly; }
        }

        /// <summary>
        /// Diagnostics worth counting in the report. Internal errors are not among them: they
        /// become SCAN.INTERNAL_ERROR findings instead.
        /// </summary>
        internal int DiagnosticCount { get; }

        /// <summary>The first internal error, or null. The scan reports it as a real failure.</summary>
        internal LexDiagnostic InternalErrorDiagnostic { get; }

        /// <summary>The first diagnostic after which the file was not parsed as code, or null.</summary>
        internal LexDiagnostic FirstTruncatingDiagnostic { get; }

        /// <summary>
        /// The one answer to "is this position editor only": the file as a whole, or the branch
        /// the position sits in. Checks ask this and nothing else.
        /// </summary>
        internal bool IsEditorOnlyAt(int start)
        {
            return File.EditorOnly || Regions.IsEditorOnlyAt(start);
        }

        internal static SourceFile FromText(ScopedFile file, string text)
        {
            LexResult lex = CSharpLexer.Lex(text);
            ConditionalRegions regions = ConditionalRegionBuilder.Build(lex);

            return new SourceFile(
                file,
                lex,
                regions,
                UsingContextBuilder.Build(lex.Tokens),
                DeclaredNameCollector.Collect(lex.Tokens));
        }

        private static int CountReportable(IReadOnlyList<LexDiagnostic> diagnostics)
        {
            int count = 0;
            foreach (LexDiagnostic diagnostic in diagnostics)
            {
                if (diagnostic.Code != LexDiagnosticCode.InternalError)
                {
                    count++;
                }
            }

            return count;
        }

        private static LexDiagnostic FirstOf(LexResult lex, ConditionalRegions regions, params LexDiagnosticCode[] codes)
        {
            LexDiagnostic best = Earliest(null, lex.Diagnostics, codes);
            return Earliest(best, regions.Diagnostics, codes);
        }

        private static LexDiagnostic Earliest(
            LexDiagnostic best, IReadOnlyList<LexDiagnostic> diagnostics, LexDiagnosticCode[] codes)
        {
            foreach (LexDiagnostic diagnostic in diagnostics)
            {
                if (!Matches(diagnostic.Code, codes))
                {
                    continue;
                }

                if (best == null ||
                    diagnostic.Line < best.Line ||
                    (diagnostic.Line == best.Line && diagnostic.Column < best.Column))
                {
                    best = diagnostic;
                }
            }

            return best;
        }

        private static bool Matches(LexDiagnosticCode code, LexDiagnosticCode[] codes)
        {
            foreach (LexDiagnosticCode candidate in codes)
            {
                if (candidate == code)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
