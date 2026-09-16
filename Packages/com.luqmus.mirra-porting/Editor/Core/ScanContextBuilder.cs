using System;
using System.Collections.Generic;
using Luqmus.MirraPorting.Code;

namespace Luqmus.MirraPorting.Core
{
    /// <summary>
    /// Reads every file in scope and assembles what the checks run on. Step 6 replaces this loop
    /// with one that reports progress and can be cancelled, using the same loader.
    /// </summary>
    internal static class ScanContextBuilder
    {
        internal static ScanContextBuildResult Build(IProjectEnvironment environment, IReadOnlyList<ScopedFile> files)
        {
            if (environment == null)
            {
                throw new ArgumentNullException(nameof(environment));
            }

            if (files == null)
            {
                throw new ArgumentNullException(nameof(files));
            }

            var sources = new List<SourceFile>(files.Count);
            var errors = new List<ScanError>();
            var declaredNames = new HashSet<string>(StringComparer.Ordinal);

            foreach (ScopedFile file in files)
            {
                SourceFile source;
                ScanError error;
                if (!SourceFileLoader.TryLoad(file, out source, out error))
                {
                    errors.Add(error);
                    continue;
                }

                sources.Add(source);

                foreach (string name in source.DeclaredNames)
                {
                    declaredNames.Add(name);
                }

                LexDiagnostic internalError = source.InternalErrorDiagnostic;
                if (internalError != null)
                {
                    // A crash inside the lexer is a scanner failure, not something the port has to
                    // fix, so it becomes SCAN.INTERNAL_ERROR rather than a diagnostic count.
                    errors.Add(new ScanError(
                        source.ProjectRelativePath,
                        "Сбой разбора файла на строке " + internalError.Line + ", часть файла не просканирована"));
                }
            }

            return new ScanContextBuildResult(new ScanContext(environment, sources, declaredNames), errors);
        }
    }
}
