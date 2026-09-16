using System;

namespace Sandbox.Tests
{
    /// <summary>
    /// report.json as this test reads it. Deliberately its own type rather than the scanner's: the
    /// check is that the file the scanner writes says what we expect, not that the scanner agrees
    /// with itself. JsonUtility ignores the fields that are not listed here.
    /// </summary>
    [Serializable]
    internal sealed class SandboxReportDto
    {
        public SandboxFindingDto[] findings = new SandboxFindingDto[0];
    }

    [Serializable]
    internal sealed class SandboxFindingDto
    {
        public string ruleId = string.Empty;
        public string severity = string.Empty;
        public string confidence = string.Empty;
        public bool editorOnly;
        public string path = string.Empty;
        public int line;
    }
}
