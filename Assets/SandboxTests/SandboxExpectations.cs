using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Sandbox.Tests
{
    /// <summary>
    /// Reads what the sandbox says it expects. Every deliberate violation in Assets/Sandbox carries
    /// a comment next to it:
    ///
    ///     Time.timeScale = 0f;   // EXPECT API.TIMESCALE_WRITE Error High false
    ///
    /// so the expectation lives next to the code it describes instead of in a table that drifts
    /// away from it. EXPECTED.md is generated from the same comments.
    /// </summary>
    internal static class SandboxExpectations
    {
        /// <summary>Only findings from here are compared; Assets/SandboxTests is not the sandbox.</summary>
        internal const string SandboxPrefix = "Assets/Sandbox/";

        private static readonly Regex ExpectPattern =
            new Regex(@"//\s*EXPECT\s+(\S+)\s+(\S+)\s+(\S+)\s+(\S+)", RegexOptions.CultureInvariant);

        internal static bool IsInSandbox(string projectRelativePath)
        {
            return projectRelativePath != null &&
                   projectRelativePath.StartsWith(SandboxPrefix, StringComparison.Ordinal);
        }

        /// <summary>One finding as a line that can be compared and printed.</summary>
        internal static string Key(string path, int line, string ruleId, string severity, string confidence, bool editorOnly)
        {
            return path + ":" + line + " " + ruleId + " " + severity + " " + confidence + " " +
                   (editorOnly ? "editor-only" : "shipped");
        }

        /// <summary>Everything the sandbox sources declare they expect.</summary>
        internal static SortedSet<string> Read()
        {
            var expected = new SortedSet<string>(StringComparer.Ordinal);
            string root = Path.Combine(Application.dataPath, "Sandbox");

            foreach (string file in Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories))
            {
                string projectRelativePath = ToProjectRelativePath(file);
                string[] lines = File.ReadAllLines(file);

                for (int i = 0; i < lines.Length; i++)
                {
                    Match match = ExpectPattern.Match(lines[i]);
                    if (!match.Success)
                    {
                        continue;
                    }

                    expected.Add(Key(
                        projectRelativePath,
                        i + 1,
                        match.Groups[1].Value,
                        match.Groups[2].Value,
                        match.Groups[3].Value,
                        string.Equals(match.Groups[4].Value, "true", StringComparison.OrdinalIgnoreCase)));
                }
            }

            return expected;
        }

        private static string ToProjectRelativePath(string absolutePath)
        {
            string assets = Application.dataPath.Replace('\\', '/');
            string normalized = absolutePath.Replace('\\', '/');

            return "Assets" + normalized.Substring(assets.Length);
        }
    }
}
