using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Luqmus.MirraPorting.Core
{
    /// <summary>
    /// Decides which C# files the scanner reads: the game code under Assets and in embedded or
    /// local packages, without the porting package itself, without MirraSDK, and without folders
    /// Unity does not import.
    /// </summary>
    internal sealed class ScanScope
    {
        /// <summary>Packages that are part of the porting setup rather than of the game.</summary>
        internal static readonly string[] DefaultExcludedPackages =
        {
            "com.luqmus.mirra-porting",
            "com.romanlee17.mirrasdk5",
        };

        private const string CSharpExtension = ".cs";
        private const string AsmdefExtension = ".asmdef";
        private const string EditorFolderName = "Editor";

        private readonly IProjectEnvironment _environment;
        private readonly HashSet<string> _excludedPackages;

        internal ScanScope(IProjectEnvironment environment)
            : this(environment, DefaultExcludedPackages)
        {
        }

        internal ScanScope(IProjectEnvironment environment, IEnumerable<string> excludedPackages)
        {
            if (environment == null)
            {
                throw new ArgumentNullException(nameof(environment));
            }

            _environment = environment;

            // Package names are lower case by convention but nothing enforces it, and paths on
            // Windows are case insensitive anyway.
            _excludedPackages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (excludedPackages != null)
            {
                foreach (string name in excludedPackages)
                {
                    if (!string.IsNullOrEmpty(name))
                    {
                        _excludedPackages.Add(name);
                    }
                }
            }
        }

        internal ScanScopeResult Collect()
        {
            var files = new List<ScopedFile>();
            var errors = new List<ScanError>();

            foreach (ScanRoot root in _environment.GetScanRoots())
            {
                if (!string.IsNullOrEmpty(root.PackageName) && _excludedPackages.Contains(root.PackageName))
                {
                    continue;
                }

                CollectRoot(root, files, errors);
            }

            List<ScopedFile> ordered = files
                .OrderBy(f => f.ProjectRelativePath, StringComparer.OrdinalIgnoreCase)
                .ToList();

            return new ScanScopeResult(ordered, errors);
        }

        private void CollectRoot(ScanRoot root, List<ScopedFile> files, List<ScanError> errors)
        {
            if (!Directory.Exists(root.AbsolutePath))
            {
                errors.Add(new ScanError(root.ProjectRelativePath, "Папка не найдена на диске: " + root.AbsolutePath));
                return;
            }

            var pending = new Stack<string>();
            pending.Push(root.AbsolutePath);

            while (pending.Count > 0)
            {
                string directory = pending.Pop();

                string[] entries;
                try
                {
                    entries = Directory.GetFiles(directory);
                }
                catch (Exception exception)
                {
                    errors.Add(DescribeFailure(root, directory, exception));
                    continue;
                }

                if (HoldsSdkAssembly(entries))
                {
                    // A copy of MirraSDK inside Assets: it and everything under it is not game code.
                    continue;
                }

                foreach (string entry in entries)
                {
                    if (IsScannableSourceFile(entry))
                    {
                        AddFile(root, entry, files, errors);
                    }
                }

                string[] subdirectories;
                try
                {
                    subdirectories = Directory.GetDirectories(directory);
                }
                catch (Exception exception)
                {
                    errors.Add(DescribeFailure(root, directory, exception));
                    continue;
                }

                foreach (string subdirectory in subdirectories)
                {
                    if (!PathUtil.IsHiddenDirectoryName(Path.GetFileName(subdirectory)))
                    {
                        pending.Push(subdirectory);
                    }
                }
            }
        }

        private void AddFile(ScanRoot root, string absolutePath, List<ScopedFile> files, List<ScanError> errors)
        {
            string projectRelativePath = PathUtil.ToProjectRelative(root, absolutePath);
            if (projectRelativePath == null)
            {
                errors.Add(new ScanError(
                    root.ProjectRelativePath,
                    "Файл оказался вне своего корня сканирования: " + PathUtil.Normalize(absolutePath)));
                return;
            }

            files.Add(new ScopedFile(projectRelativePath, PathUtil.Normalize(absolutePath), IsEditorOnly(projectRelativePath)));
        }

        /// <summary>
        /// Asks the compilation pipeline first. A file no assembly claims — excluded by an asmdef,
        /// or added after the last compilation — falls back to the Editor folder convention.
        /// </summary>
        private bool IsEditorOnly(string projectRelativePath)
        {
            if (_environment.IsKnownToAnyAssembly(projectRelativePath))
            {
                return _environment.IsEditorOnlyAssemblyFile(projectRelativePath);
            }

            return PathUtil.HasSegment(projectRelativePath, EditorFolderName);
        }

        private static bool HoldsSdkAssembly(string[] entries)
        {
            foreach (string entry in entries)
            {
                if (!HasExtension(entry, AsmdefExtension))
                {
                    continue;
                }

                if (AsmdefProbe.IsSdkAssemblyName(AsmdefProbe.ReadAssemblyName(entry)))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsScannableSourceFile(string path)
        {
            if (!HasExtension(path, CSharpExtension))
            {
                return false;
            }

            // Unity does not import files whose name starts with a dot.
            string name = Path.GetFileName(path);
            return name.Length > 0 && name[0] != '.';
        }

        private static bool HasExtension(string path, string extension)
        {
            return path.EndsWith(extension, StringComparison.OrdinalIgnoreCase);
        }

        private static ScanError DescribeFailure(ScanRoot root, string directory, Exception exception)
        {
            string relative = PathUtil.ToProjectRelative(root, directory) ?? root.ProjectRelativePath;
            return new ScanError(relative, "Не удалось прочитать папку: " + exception.GetType().Name + ": " + exception.Message);
        }
    }
}
