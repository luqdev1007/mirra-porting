using System;

namespace Luqmus.MirraPorting.Report
{
    /// <summary>
    /// The shape of report.json. These types exist only because JsonUtility serializes public
    /// fields of [Serializable] classes and nothing else: the model in Core stays immutable and
    /// free of the format.
    ///
    /// Field order here is the order in the file. Renaming a field changes the format, so anything
    /// reading an older report has to keep working — see schemaVersion.
    /// </summary>
    [Serializable]
    internal sealed class ReportDto
    {
        public int schemaVersion;
        public ToolDto tool = new ToolDto();
        public ProjectDto project = new ProjectDto();
        public string startedAt = string.Empty;
        public string finishedAt = string.Empty;
        public long durationMs;
        public string[] notices = new string[0];
        public SummaryDto summary = new SummaryDto();
        public DiagnosticsDto diagnostics = new DiagnosticsDto();
        public TruncatedFileDto[] truncatedFiles = new TruncatedFileDto[0];
        public FindingDto[] findings = new FindingDto[0];
    }

    [Serializable]
    internal sealed class ToolDto
    {
        public string name = string.Empty;
        public string version = string.Empty;
    }

    [Serializable]
    internal sealed class ProjectDto
    {
        public string path = string.Empty;
        public string unityVersion = string.Empty;
        public string activeBuildTarget = string.Empty;
        public string mirraSdkVersion = string.Empty;
    }

    [Serializable]
    internal sealed class SummaryDto
    {
        public int errors;
        public int warnings;
        public int info;
        public int editorOnly;
        public int filesScanned;
    }

    [Serializable]
    internal sealed class DiagnosticsDto
    {
        public int filesWithDiagnostics;
        public int total;
    }

    [Serializable]
    internal sealed class TruncatedFileDto
    {
        public string path = string.Empty;
        public int line;
        public string code = string.Empty;
    }

    [Serializable]
    internal sealed class FindingDto
    {
        public string ruleId = string.Empty;
        public string category = string.Empty;
        public string severity = string.Empty;
        public string confidence = string.Empty;
        public string confidenceReason = string.Empty;
        public bool editorOnly;
        public string path = string.Empty;
        public int line;
        public int column;
        public string snippet = string.Empty;
        public string message = string.Empty;
        public string suggestion = string.Empty;
    }
}
