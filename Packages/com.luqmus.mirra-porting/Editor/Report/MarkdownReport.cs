using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Luqmus.MirraPorting.Core;

namespace Luqmus.MirraPorting.Report
{
    /// <summary>
    /// report.md: the half a person reads. Findings are grouped category by category and rule by
    /// rule, so the same explanation is not repeated under every line of code.
    ///
    /// Editor only findings live in their own section at the end and are left out of the summary
    /// table, so nothing is counted twice.
    /// </summary>
    internal static class MarkdownReport
    {
        private const string DateFormat = "yyyy-MM-dd HH:mm:ss";

        internal static string Build(ScanReport report)
        {
            if (report == null)
            {
                throw new ArgumentNullException(nameof(report));
            }

            var text = new StringBuilder();

            WriteHeader(text, report);
            WriteNotices(text, report);
            WriteSummary(text, report);
            WriteFindings(text, report);
            WriteTruncatedFiles(text, report);
            WriteEditorOnly(text, report);

            return text.ToString();
        }

        private static void WriteHeader(StringBuilder text, ScanReport report)
        {
            text.AppendLine("# Отчёт Mirra Porting");
            text.AppendLine();
            text.AppendLine("- Проект: `" + report.Project.Path + "`");
            text.AppendLine("- Дата: " + report.FinishedAtUtc.ToUniversalTime().ToString(DateFormat, CultureInfo.InvariantCulture) + " UTC");
            text.AppendLine("- Unity: " + report.Project.UnityVersion);
            text.AppendLine("- Активная платформа: " + report.Project.ActiveBuildTarget);
            text.AppendLine("- MirraSDK: " + SdkLine(report));
            text.AppendLine("- Просканировано файлов: " + Number(report.Summary.FilesScanned));
            text.AppendLine("- Время: " + Number(report.DurationMs) + " мс");
            text.AppendLine();
        }

        private static string SdkLine(ScanReport report)
        {
            if (report.Project.MirraSdkVersion.Length > 0)
            {
                return report.Project.MirraSdkVersion;
            }

            if (report.SdkFoldersInAssets.Count > 0)
            {
                return "не найден как пакет, лежит в `" + report.SdkFoldersInAssets[0] + "`";
            }

            return "не установлен";
        }

        private static void WriteNotices(StringBuilder text, ScanReport report)
        {
            if (report.Notices.Count == 0)
            {
                return;
            }

            for (int i = 0; i < report.Notices.Count; i++)
            {
                if (i > 0)
                {
                    text.AppendLine(">");
                }

                text.AppendLine("> **Внимание.** " + report.Notices[i]);
            }

            text.AppendLine();
        }

        private static void WriteSummary(StringBuilder text, ScanReport report)
        {
            text.AppendLine("## Сводка");
            text.AppendLine();

            IReadOnlyList<Finding> shipped = Where(report.Findings, false);
            if (shipped.Count == 0)
            {
                text.AppendLine("В коде, который попадает в сборку игры, находок нет.");
            }
            else
            {
                text.AppendLine("| Категория | Error | Warning | Info |");
                text.AppendLine("|---|---:|---:|---:|");

                foreach (string category in Categories(shipped))
                {
                    text.AppendLine(
                        "| " + category +
                        " | " + Number(Count(shipped, category, Severity.Error)) +
                        " | " + Number(Count(shipped, category, Severity.Warning)) +
                        " | " + Number(Count(shipped, category, Severity.Info)) + " |");
                }

                text.AppendLine(
                    "| **Всего** | " + Number(report.Summary.Errors) +
                    " | " + Number(report.Summary.Warnings) +
                    " | " + Number(report.Summary.Info) + " |");
            }

            text.AppendLine();

            if (report.Summary.EditorOnly > 0)
            {
                text.AppendLine(
                    "Ещё " + Plural(report.Summary.EditorOnly, "находка", "находки", "находок") +
                    " в коде, который не попадает в сборку игры, — в разделе «Только редактор».");
                text.AppendLine();
            }

            if (report.Diagnostics.Total > 0)
            {
                text.AppendLine(
                    "Мест, которые не удалось разобрать: " + Number(report.Diagnostics.Total) +
                    " в " + Plural(report.Diagnostics.FilesWithDiagnostics, "файле", "файлах", "файлах") +
                    ". Находками они не становятся: чаще всего это неактивные ветки `#if`.");
                text.AppendLine();
            }
        }

        private static void WriteFindings(StringBuilder text, ScanReport report)
        {
            IReadOnlyList<Finding> shipped = Where(report.Findings, false);
            if (shipped.Count == 0)
            {
                return;
            }

            text.AppendLine("## Находки");
            text.AppendLine();
            WriteGroups(text, shipped);
        }

        private static void WriteEditorOnly(StringBuilder text, ScanReport report)
        {
            IReadOnlyList<Finding> editorOnly = Where(report.Findings, true);
            if (editorOnly.Count == 0)
            {
                return;
            }

            text.AppendLine("## Только редактор");
            text.AppendLine();
            text.AppendLine("Этот код не попадает в сборку игры, на модерацию он не влияет.");
            text.AppendLine();
            WriteGroups(text, editorOnly);
        }

        /// <summary>Category, then rule, in the order the sorted findings introduce them.</summary>
        private static void WriteGroups(StringBuilder text, IReadOnlyList<Finding> findings)
        {
            foreach (string category in Categories(findings))
            {
                text.AppendLine("### " + category);
                text.AppendLine();

                foreach (string ruleId in RuleIds(findings, category))
                {
                    IReadOnlyList<Finding> ofRule = OfRule(findings, category, ruleId);

                    text.AppendLine("#### " + ruleId + " — " + Number(ofRule.Count));
                    text.AppendLine();
                    text.AppendLine(ofRule[0].Message);

                    if (ofRule[0].Suggestion.Length > 0)
                    {
                        text.AppendLine();
                        text.AppendLine("Замена: " + ofRule[0].Suggestion);
                    }

                    text.AppendLine();

                    foreach (Finding finding in ofRule)
                    {
                        text.AppendLine(Line(finding));
                    }

                    text.AppendLine();
                }
            }
        }

        private static string Line(Finding finding)
        {
            var line = new StringBuilder("- ");
            line.Append(Place(finding));
            line.Append(" — ");

            // A finding without a snippet carries its own message, as SCAN.INTERNAL_ERROR does.
            line.Append(finding.Snippet.Length > 0 ? Code(finding.Snippet) : finding.Message);

            if (finding.Confidence != Confidence.High)
            {
                line.Append(" (low confidence");
                if (finding.ConfidenceReason.Length > 0)
                {
                    line.Append(": ").Append(finding.ConfidenceReason);
                }

                line.Append(')');
            }

            return line.ToString();
        }

        private static string Place(Finding finding)
        {
            if (finding.Path.Length == 0)
            {
                return "весь проект";
            }

            return finding.Line > 0
                ? "`" + finding.Path + ":" + Number(finding.Line) + "`"
                : "`" + finding.Path + "`";
        }

        /// <summary>
        /// Inline code. A snippet that holds a backtick needs a longer fence and a space inside it,
        /// otherwise Markdown breaks in the middle of the line of code.
        /// </summary>
        private static string Code(string snippet)
        {
            return snippet.IndexOf('`') >= 0 ? "`` " + snippet + " ``" : "`" + snippet + "`";
        }

        private static void WriteTruncatedFiles(StringBuilder text, ScanReport report)
        {
            if (report.TruncatedFiles.Count == 0)
            {
                return;
            }

            text.AppendLine("## Разобраны не полностью");
            text.AppendLine();
            text.AppendLine(
                "С указанной строки файл перестал читаться как код: незакрытый комментарий, " +
                "verbatim-строка или `#if`. Находки ниже этой строки потеряны, файл стоит посмотреть глазами.");
            text.AppendLine();

            foreach (TruncatedFile file in report.TruncatedFiles)
            {
                text.AppendLine("- `" + file.Path + ":" + Number(file.Line) + "` — " + file.Code);
            }

            text.AppendLine();
        }

        private static IReadOnlyList<Finding> Where(IReadOnlyList<Finding> findings, bool editorOnly)
        {
            var result = new List<Finding>();
            foreach (Finding finding in findings)
            {
                if (finding.EditorOnly == editorOnly)
                {
                    result.Add(finding);
                }
            }

            return result;
        }

        private static IReadOnlyList<string> Categories(IReadOnlyList<Finding> findings)
        {
            var categories = new List<string>();
            foreach (Finding finding in findings)
            {
                if (!categories.Contains(finding.Category))
                {
                    categories.Add(finding.Category);
                }
            }

            return categories;
        }

        private static IReadOnlyList<string> RuleIds(IReadOnlyList<Finding> findings, string category)
        {
            var ruleIds = new List<string>();
            foreach (Finding finding in findings)
            {
                if (finding.Category == category && !ruleIds.Contains(finding.RuleId))
                {
                    ruleIds.Add(finding.RuleId);
                }
            }

            return ruleIds;
        }

        private static IReadOnlyList<Finding> OfRule(IReadOnlyList<Finding> findings, string category, string ruleId)
        {
            var result = new List<Finding>();
            foreach (Finding finding in findings)
            {
                if (finding.Category == category && finding.RuleId == ruleId)
                {
                    result.Add(finding);
                }
            }

            return result;
        }

        private static int Count(IReadOnlyList<Finding> findings, string category, Severity severity)
        {
            int count = 0;
            foreach (Finding finding in findings)
            {
                if (finding.Category == category && finding.Severity == severity)
                {
                    count++;
                }
            }

            return count;
        }

        private static string Number(long value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// A count with the noun in the right form: 1 находка, 2 находки, 5 находок. The report is
        /// read by colleagues, and "1 находок" reads like a machine wrote it.
        /// </summary>
        private static string Plural(int count, string one, string few, string many)
        {
            int lastTwo = count % 100;
            int last = count % 10;

            if (lastTwo >= 11 && lastTwo <= 14)
            {
                return Number(count) + " " + many;
            }

            if (last == 1)
            {
                return Number(count) + " " + one;
            }

            return last >= 2 && last <= 4 ? Number(count) + " " + few : Number(count) + " " + many;
        }
    }
}
