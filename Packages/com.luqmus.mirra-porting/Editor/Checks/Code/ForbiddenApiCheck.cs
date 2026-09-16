using System;
using System.Collections.Generic;
using Luqmus.MirraPorting.Code;
using Luqmus.MirraPorting.Core;

namespace Luqmus.MirraPorting.Checks
{
    /// <summary>
    /// Finds the Unity API that MirraSDK has to own: Time.timeScale, PlayerPrefs, Application.Quit
    /// and the rest of the table.
    ///
    /// One pass over the tokens of each file. A file that throws costs a SCAN.INTERNAL_ERROR
    /// finding, not the scan.
    /// </summary>
    internal sealed class ForbiddenApiCheck : IScanCheck
    {
        public string Id
        {
            get { return "ForbiddenApi"; }
        }

        public void Run(ScanContext context, IFindingSink sink)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (sink == null)
            {
                throw new ArgumentNullException(nameof(sink));
            }

            foreach (SourceFile file in context.Files)
            {
                try
                {
                    Scan(file, context.DeclaredTypes, sink);
                }
                catch (Exception exception)
                {
                    sink.Add(Finding.FromRule(
                        Rules.ScanInternalError,
                        file.ProjectRelativePath,
                        0,
                        0,
                        string.Empty,
                        Confidence.High,
                        string.Empty,
                        false,
                        "Проверка ForbiddenApi не смогла разобрать файл: " +
                        exception.GetType().Name + ": " + exception.Message));
                }
            }
        }

        private static void Scan(SourceFile file, DeclaredTypeIndex declaredTypes, IFindingSink sink)
        {
            FileTypeIndex typeIndex = FileTypeIndex.Build(file, declaredTypes);
            if (typeIndex.IsEmpty)
            {
                return;
            }

            IReadOnlyList<Token> tokens = file.Lex.Tokens;
            for (int i = 0; i < tokens.Count; i++)
            {
                ChainMatch match;
                if (ChainMatcher.TryMatchAt(tokens, i, typeIndex, out match))
                {
                    sink.Add(BuildFinding(file, match));
                }
            }
        }

        private static Finding BuildFinding(SourceFile file, ChainMatch match)
        {
            Rule rule = Rules.Find(match.Member.RuleIdFor(match.Access));
            string snippet = SnippetBuilder.Build(file.Lex, match.FirstToken.Line, match.DeciderToken.Line);

            return Finding.FromRule(
                rule,
                file.ProjectRelativePath,
                match.FirstToken.Line,
                match.FirstToken.Column,
                snippet,
                match.Type.Confidence,
                match.Type.Reason,
                file.IsEditorOnlyAt(match.FirstToken.Start));
        }
    }
}
