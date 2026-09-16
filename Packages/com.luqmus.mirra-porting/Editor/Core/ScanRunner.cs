using System;
using System.Collections.Generic;
using System.Diagnostics;
using Luqmus.MirraPorting.Code;

namespace Luqmus.MirraPorting.Core
{
    /// <summary>
    /// Runs one scan: walks the project, reads the files, runs the checks and assembles the
    /// report. Reports progress and asks whether it should stop between files and between checks.
    ///
    /// Nothing here throws at the caller. A folder it cannot read, a file it cannot open and a
    /// check that crashes all become SCAN.INTERNAL_ERROR findings, and the scan goes on.
    /// </summary>
    internal static class ScanRunner
    {
        private const string ReadingStage = "Чтение файлов";
        private const string CheckingStage = "Проверки";

        internal static ScanRunResult Run(IProjectEnvironment environment, ScanOptions options)
        {
            if (environment == null)
            {
                throw new ArgumentNullException(nameof(environment));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            DateTime startedAt = DateTime.UtcNow;
            Stopwatch stopwatch = Stopwatch.StartNew();

            var notices = new List<string>();
            var errors = new List<ScanError>();

            ScanScopeResult scope = new ScanScope(environment).Collect();
            errors.AddRange(scope.Errors);
            AddNotices(notices, environment, scope);

            var sources = new List<SourceFile>(scope.Files.Count);
            if (!ReadFiles(scope, options, sources, errors))
            {
                return ScanRunResult.Cancelled;
            }

            var collector = new FindingCollector();
            foreach (ScanError error in errors)
            {
                collector.Add(InternalError(error.Path, 0, 0, error.Message));
            }

            ReportInternalErrors(sources, collector);

            if (!RunChecks(BuildContext(environment, sources), options, collector))
            {
                return ScanRunResult.Cancelled;
            }

            stopwatch.Stop();

            IReadOnlyList<Finding> findings = collector.Sorted();
            var report = new ScanReport(
                ProjectInfo.FromEnvironment(environment),
                startedAt,
                DateTime.UtcNow,
                stopwatch.ElapsedMilliseconds,
                notices,
                ScanSummary.FromFindings(findings, sources.Count),
                CountDiagnostics(sources),
                CollectTruncatedFiles(sources),
                findings);

            return ScanRunResult.Completed(report);
        }

        private static void AddNotices(List<string> notices, IProjectEnvironment environment, ScanScopeResult scope)
        {
            if (!ScanNotices.IsWebGl(environment.ActiveBuildTarget))
            {
                notices.Add(ScanNotices.NonWebGlPlatform(environment.ActiveBuildTarget));
            }

            foreach (string folder in scope.ExcludedSdkFolders)
            {
                notices.Add(ScanNotices.SdkInsideAssets(folder));
            }
        }

        /// <summary>Reads every file in scope. Returns false when the scan was cancelled.</summary>
        private static bool ReadFiles(
            ScanScopeResult scope, ScanOptions options, List<SourceFile> sources, List<ScanError> errors)
        {
            for (int i = 0; i < scope.Files.Count; i++)
            {
                if (IsCancelled(options))
                {
                    return false;
                }

                ScopedFile file = scope.Files[i];
                Report(options, ReadingStage, file.ProjectRelativePath, (float)i / Math.Max(1, scope.Files.Count));

                SourceFile source;
                ScanError error;
                if (SourceFileLoader.TryLoad(file, out source, out error))
                {
                    sources.Add(source);
                }
                else
                {
                    errors.Add(error);
                }
            }

            return true;
        }

        /// <summary>
        /// A crash inside the lexer is a scanner failure rather than something the port has to fix,
        /// so it is reported as a finding at the position it happened, not counted as a diagnostic.
        /// </summary>
        private static void ReportInternalErrors(List<SourceFile> sources, IFindingSink sink)
        {
            foreach (SourceFile source in sources)
            {
                LexDiagnostic diagnostic = source.InternalErrorDiagnostic;
                if (diagnostic == null)
                {
                    continue;
                }

                sink.Add(InternalError(
                    source.ProjectRelativePath,
                    diagnostic.Line,
                    diagnostic.Column,
                    "Сбой разбора файла: часть файла не просканирована"));
            }
        }

        private static ScanContext BuildContext(IProjectEnvironment environment, List<SourceFile> sources)
        {
            var declaredNames = new List<DeclaredName>();
            foreach (SourceFile source in sources)
            {
                foreach (DeclaredName name in source.Declarations.Names)
                {
                    declaredNames.Add(name);
                }
            }

            return new ScanContext(environment, sources, DeclaredTypeIndex.Build(declaredNames));
        }

        /// <summary>Runs every check. Returns false when the scan was cancelled.</summary>
        private static bool RunChecks(ScanContext context, ScanOptions options, IFindingSink sink)
        {
            IReadOnlyList<IScanCheck> checks = ScanChecks.Create();

            for (int i = 0; i < checks.Count; i++)
            {
                if (IsCancelled(options))
                {
                    return false;
                }

                IScanCheck check = checks[i];
                Report(options, CheckingStage, check.Id, (float)i / Math.Max(1, checks.Count));

                try
                {
                    check.Run(context, sink);
                }
                catch (Exception exception)
                {
                    sink.Add(InternalError(
                        string.Empty,
                        0,
                        0,
                        "Проверка " + check.Id + " завершилась с ошибкой: " +
                        exception.GetType().Name + ": " + exception.Message));
                }
            }

            return true;
        }

        private static DiagnosticsSummary CountDiagnostics(List<SourceFile> sources)
        {
            int files = 0;
            int total = 0;

            foreach (SourceFile source in sources)
            {
                if (source.DiagnosticCount > 0)
                {
                    files++;
                    total += source.DiagnosticCount;
                }
            }

            return new DiagnosticsSummary(files, total);
        }

        private static IReadOnlyList<TruncatedFile> CollectTruncatedFiles(List<SourceFile> sources)
        {
            var truncated = new List<TruncatedFile>();

            foreach (SourceFile source in sources)
            {
                LexDiagnostic diagnostic = source.FirstTruncatingDiagnostic;
                if (diagnostic != null)
                {
                    truncated.Add(new TruncatedFile(
                        source.ProjectRelativePath, diagnostic.Line, diagnostic.Code.ToString()));
                }
            }

            return truncated;
        }

        private static Finding InternalError(string path, int line, int column, string message)
        {
            return Finding.FromRule(
                Rules.ScanInternalError, path, line, column, string.Empty, Confidence.High, string.Empty, false, message);
        }

        private static bool IsCancelled(ScanOptions options)
        {
            return options.IsCancelled != null && options.IsCancelled();
        }

        private static void Report(ScanOptions options, string stage, string detail, float fraction)
        {
            if (options.OnProgress != null)
            {
                options.OnProgress(new ScanProgressInfo(stage, detail, fraction));
            }
        }
    }
}
