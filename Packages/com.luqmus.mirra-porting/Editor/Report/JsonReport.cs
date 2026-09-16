using System;
using System.Collections.Generic;
using System.Globalization;
using Luqmus.MirraPorting.Core;
using UnityEngine;

namespace Luqmus.MirraPorting.Report
{
    /// <summary>
    /// report.json: the machine readable half of the report. Written for an agent to read, and
    /// read back by the window to show the last scan after a domain reload.
    /// </summary>
    internal static class JsonReport
    {
        internal const int SchemaVersion = 1;

        /// <summary>ISO 8601 in UTC, so two machines in different time zones write the same string.</summary>
        private const string DateFormat = "yyyy-MM-ddTHH:mm:ssZ";

        internal static string ToJson(ScanReport report)
        {
            if (report == null)
            {
                throw new ArgumentNullException(nameof(report));
            }

            return JsonUtility.ToJson(ToDto(report), true);
        }

        /// <summary>
        /// Reads a report back. Returns false for anything that is not a report; a report from a
        /// future version, or with a severity this build does not know, still reads.
        /// </summary>
        internal static bool TryParse(string json, out ScanReport report)
        {
            report = null;
            if (string.IsNullOrEmpty(json))
            {
                return false;
            }

            ReportDto dto;
            try
            {
                dto = JsonUtility.FromJson<ReportDto>(json);
            }
            catch (Exception)
            {
                return false;
            }

            if (dto == null || dto.summary == null || dto.project == null)
            {
                return false;
            }

            report = FromDto(dto);
            return true;
        }

        private static ReportDto ToDto(ScanReport report)
        {
            var dto = new ReportDto
            {
                schemaVersion = SchemaVersion,
                tool = new ToolDto { name = report.Project.ToolName, version = report.Project.ToolVersion },
                project = new ProjectDto
                {
                    path = report.Project.Path,
                    unityVersion = report.Project.UnityVersion,
                    activeBuildTarget = report.Project.ActiveBuildTarget,
                    mirraSdkVersion = report.Project.MirraSdkVersion,
                },
                startedAt = FormatDate(report.StartedAtUtc),
                finishedAt = FormatDate(report.FinishedAtUtc),
                durationMs = report.DurationMs,
                notices = ToArray(report.Notices),
                summary = new SummaryDto
                {
                    errors = report.Summary.Errors,
                    warnings = report.Summary.Warnings,
                    info = report.Summary.Info,
                    editorOnly = report.Summary.EditorOnly,
                    filesScanned = report.Summary.FilesScanned,
                },
                diagnostics = new DiagnosticsDto
                {
                    filesWithDiagnostics = report.Diagnostics.FilesWithDiagnostics,
                    total = report.Diagnostics.Total,
                },
            };

            var truncated = new TruncatedFileDto[report.TruncatedFiles.Count];
            for (int i = 0; i < truncated.Length; i++)
            {
                TruncatedFile file = report.TruncatedFiles[i];
                truncated[i] = new TruncatedFileDto { path = file.Path, line = file.Line, code = file.Code };
            }

            dto.truncatedFiles = truncated;

            var findings = new FindingDto[report.Findings.Count];
            for (int i = 0; i < findings.Length; i++)
            {
                findings[i] = ToDto(report.Findings[i]);
            }

            dto.findings = findings;
            return dto;
        }

        private static FindingDto ToDto(Finding finding)
        {
            return new FindingDto
            {
                ruleId = finding.RuleId,
                category = finding.Category,
                severity = finding.Severity.ToString(),
                confidence = finding.Confidence.ToString(),
                confidenceReason = finding.ConfidenceReason,
                editorOnly = finding.EditorOnly,
                path = finding.Path,
                line = finding.Line,
                column = finding.Column,
                snippet = finding.Snippet,
                message = finding.Message,
                suggestion = finding.Suggestion,
            };
        }

        private static ScanReport FromDto(ReportDto dto)
        {
            var project = new ProjectInfo(
                dto.project.path,
                dto.project.unityVersion,
                dto.project.activeBuildTarget,
                dto.project.mirraSdkVersion,
                dto.tool != null ? dto.tool.name : string.Empty,
                dto.tool != null ? dto.tool.version : string.Empty);

            var truncated = new List<TruncatedFile>();
            if (dto.truncatedFiles != null)
            {
                foreach (TruncatedFileDto file in dto.truncatedFiles)
                {
                    truncated.Add(new TruncatedFile(file.path, file.line, file.code));
                }
            }

            var findings = new List<Finding>();
            if (dto.findings != null)
            {
                foreach (FindingDto finding in dto.findings)
                {
                    findings.Add(FromDto(finding));
                }
            }

            return new ScanReport(
                project,
                ParseDate(dto.startedAt),
                ParseDate(dto.finishedAt),
                dto.durationMs,
                dto.notices ?? new string[0],
                new ScanSummary(
                    dto.summary.errors,
                    dto.summary.warnings,
                    dto.summary.info,
                    dto.summary.editorOnly,
                    dto.summary.filesScanned),
                new DiagnosticsSummary(
                    dto.diagnostics != null ? dto.diagnostics.filesWithDiagnostics : 0,
                    dto.diagnostics != null ? dto.diagnostics.total : 0),
                truncated,
                findings);
        }

        private static Finding FromDto(FindingDto dto)
        {
            return new Finding(
                dto.ruleId,
                dto.category,
                ParseSeverity(dto.severity),
                ParseConfidence(dto.confidence),
                dto.confidenceReason,
                dto.editorOnly,
                dto.path,
                dto.line,
                dto.column,
                dto.snippet,
                dto.message,
                dto.suggestion);
        }

        /// <summary>A severity this build does not know is shown as information, not dropped.</summary>
        private static Severity ParseSeverity(string text)
        {
            if (string.Equals(text, "Error", StringComparison.Ordinal))
            {
                return Severity.Error;
            }

            return string.Equals(text, "Warning", StringComparison.Ordinal) ? Severity.Warning : Severity.Info;
        }

        /// <summary>An unknown confidence is treated as the weakest one.</summary>
        private static Confidence ParseConfidence(string text)
        {
            if (string.Equals(text, "High", StringComparison.Ordinal))
            {
                return Confidence.High;
            }

            return string.Equals(text, "Medium", StringComparison.Ordinal) ? Confidence.Medium : Confidence.Low;
        }

        private static string FormatDate(DateTime value)
        {
            return value.ToUniversalTime().ToString(DateFormat, CultureInfo.InvariantCulture);
        }

        private static DateTime ParseDate(string text)
        {
            DateTime value;
            if (DateTime.TryParseExact(
                    text,
                    DateFormat,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal,
                    out value))
            {
                return value;
            }

            return default(DateTime);
        }

        private static string[] ToArray(IReadOnlyList<string> values)
        {
            var array = new string[values.Count];
            for (int i = 0; i < values.Count; i++)
            {
                array[i] = values[i];
            }

            return array;
        }
    }
}
