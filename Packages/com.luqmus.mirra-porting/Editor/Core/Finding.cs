using System;

namespace Luqmus.MirraPorting.Core
{
    /// <summary>
    /// One problem found at one place in the project. Immutable: checks build findings and hand
    /// them to a sink, nothing edits them afterwards.
    /// </summary>
    internal sealed class Finding
    {
        /// <summary>Snippets longer than this are cut, the last character becoming an ellipsis.</summary>
        internal const int MaxSnippetLength = 160;

        internal Finding(
            string ruleId,
            string category,
            Severity severity,
            Confidence confidence,
            bool editorOnly,
            string path,
            int line,
            int column,
            string snippet,
            string message,
            string suggestion)
        {
            RuleId = ruleId;
            Category = category;
            Severity = severity;
            Confidence = confidence;
            EditorOnly = editorOnly;
            Path = path;
            Line = line;
            Column = column;
            Snippet = snippet;
            Message = message;
            Suggestion = suggestion;
        }

        internal string RuleId { get; }

        internal string Category { get; }

        internal Severity Severity { get; }

        internal Confidence Confidence { get; }

        /// <summary>File belongs to an editor only assembly, or the code sits under #if UNITY_EDITOR.</summary>
        internal bool EditorOnly { get; }

        /// <summary>Project relative path with forward slashes: "Assets/…" or "Packages/&lt;name&gt;/…".</summary>
        internal string Path { get; }

        /// <summary>1-based line of the first token of the match.</summary>
        internal int Line { get; }

        /// <summary>1-based column of the first token of the match.</summary>
        internal int Column { get; }

        /// <summary>Source line, trimmed and cut to <see cref="MaxSnippetLength"/>.</summary>
        internal string Snippet { get; }

        internal string Message { get; }

        internal string Suggestion { get; }

        /// <summary>
        /// Builds a finding from a rule, applying the two rules that hold for every check:
        /// editor only code is never worse than <see cref="Severity.Info"/>, and the snippet is
        /// trimmed and cut.
        /// </summary>
        /// <param name="messageOverride">Replaces the rule message when not null, for rules whose
        /// text depends on the situation (SCAN.INTERNAL_ERROR carries the exception text).</param>
        internal static Finding FromRule(
            Rule rule,
            string path,
            int line,
            int column,
            string snippet,
            Confidence confidence,
            bool editorOnly,
            string messageOverride = null)
        {
            if (rule == null)
            {
                throw new ArgumentNullException(nameof(rule));
            }

            Severity severity = editorOnly && rule.Severity != Severity.Info ? Severity.Info : rule.Severity;

            return new Finding(
                rule.Id,
                rule.Category,
                severity,
                confidence,
                editorOnly,
                path ?? string.Empty,
                line,
                column,
                TrimSnippet(snippet),
                messageOverride ?? rule.Message,
                rule.Suggestion);
        }

        private static string TrimSnippet(string snippet)
        {
            if (string.IsNullOrEmpty(snippet))
            {
                return string.Empty;
            }

            string trimmed = snippet.Trim();
            if (trimmed.Length <= MaxSnippetLength)
            {
                return trimmed;
            }

            return trimmed.Substring(0, MaxSnippetLength - 1) + "…";
        }
    }
}
