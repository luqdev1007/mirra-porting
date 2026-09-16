using System;
using System.Globalization;
using Luqmus.MirraPorting.Core;
using UnityEditor;
using UnityEngine;

namespace Luqmus.MirraPorting.UI
{
    /// <summary>
    /// The menu item an agent uses: it asks nothing, waits for nothing and says what it did in one
    /// line of the console.
    /// </summary>
    internal static class ScannerMenu
    {
        private const string LogPrefix = "[MirraPorting] ";

        [MenuItem("Tools/Mirra Porting/Scan and Export")]
        private static void ScanAndExport()
        {
            string outputDirectory = MirraPortingScanner.DefaultOutputDirectory();
            var options = new ScanOptions(outputDirectory);

            // The progress bar lives here rather than in the scanner, so that a scan started from a
            // test or from -executeMethod draws no UI. This path has no cancel button on purpose:
            // an agent has nobody to press it.
            options.OnProgress = ShowProgress;

            try
            {
                ScanRunResult result = MirraPortingScanner.Export(options);
                if (result == null)
                {
                    Debug.LogError(LogPrefix + "Scan failed: отчёт не записан, причина выше.");
                    return;
                }

                Debug.Log(LogPrefix + Describe(result.Report, outputDirectory));
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private static void ShowProgress(ScanProgressInfo progress)
        {
            EditorUtility.DisplayProgressBar("Mirra Porting", progress.Stage + ": " + progress.Detail, progress.Fraction);
        }

        private static string Describe(ScanReport report, string outputDirectory)
        {
            return "Scan finished: " +
                   Number(report.Summary.Errors) + " errors, " +
                   Number(report.Summary.Warnings) + " warnings, " +
                   Number(report.Summary.Info) + " info, " +
                   Number(report.Summary.EditorOnly) + " editor-only, " +
                   Number(report.Summary.FilesScanned) + " files, " +
                   Number(report.DurationMs) + " ms → " +
                   ForConsole(MirraPortingScanner.JsonPath(outputDirectory));
        }

        /// <summary>Inside the project the path reads better relative to its root.</summary>
        private static string ForConsole(string path)
        {
            string normalized = PathUtil.Normalize(path);
            string root = PathUtil.Normalize(MirraPortingScanner.ProjectRootPath()) + "/";

            return normalized.StartsWith(root, StringComparison.OrdinalIgnoreCase)
                ? normalized.Substring(root.Length)
                : normalized;
        }

        private static string Number(long value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }
    }
}
