using System;
using System.IO;
using Luqmus.MirraPorting.Core;
using Luqmus.MirraPorting.Report;
using UnityEngine;

namespace Luqmus.MirraPorting
{
    /// <summary>
    /// The way anything outside the package starts a scan: the menu item, a test, or Unity started
    /// with -executeMethod. Everything else in the package is internal on purpose — the package is
    /// copied into a port, used, and deleted, so it must not become something the game can call.
    /// </summary>
    public static class MirraPortingScanner
    {
        private const string DefaultOutputFolder = "Library/MirraPorting";

        /// <summary>
        /// Scans the project and writes the report to Library/MirraPorting.
        /// </summary>
        /// <returns>Path to report.json, or null when the scan could not be written.</returns>
        public static string ScanAndExport()
        {
            return ScanAndExport(DefaultOutputDirectory());
        }

        /// <summary>
        /// Scans the project and writes the report to the given folder.
        /// </summary>
        /// <returns>Path to report.json, or null when the scan was cancelled or could not be written.</returns>
        public static string ScanAndExport(string outputDirectory)
        {
            ScanRunResult result = Export(new ScanOptions(outputDirectory));
            return result != null && !result.WasCancelled ? JsonPath(outputDirectory) : null;
        }

        /// <summary>
        /// Runs a scan and writes it, for callers inside the package that have progress to draw or
        /// a reason to cancel. Returns null when writing failed, which is already logged.
        /// </summary>
        internal static ScanRunResult Export(ScanOptions options)
        {
            ScanRunResult result = ScanRunner.Run(new UnityProjectEnvironment(), options);
            if (result.WasCancelled)
            {
                // A cancelled scan leaves the previous report alone.
                return result;
            }

            try
            {
                ReportWriter.PrepareDirectory(options.OutputDirectory);

                // Markdown first, report.json last: the presence of the JSON means a finished scan.
                ReportWriter.WriteText(
                    Path.Combine(options.OutputDirectory, ReportWriter.MarkdownFileName),
                    MarkdownReport.Build(result.Report));
                ReportWriter.WriteText(JsonPath(options.OutputDirectory), JsonReport.ToJson(result.Report));
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    "[MirraPorting] Не удалось записать отчёт в " + options.OutputDirectory + ": " +
                    exception.GetType().Name + ": " + exception.Message);
                return null;
            }

            return result;
        }

        /// <summary>Library/MirraPorting of this project, where the menu item writes.</summary>
        internal static string DefaultOutputDirectory()
        {
            return Path.Combine(ProjectRootPath(), DefaultOutputFolder);
        }

        internal static string JsonPath(string outputDirectory)
        {
            return Path.Combine(outputDirectory, ReportWriter.JsonFileName);
        }

        /// <summary>The folder that holds Assets, not whatever the process happens to sit in.</summary>
        internal static string ProjectRootPath()
        {
            return Path.GetDirectoryName(Application.dataPath);
        }
    }
}
