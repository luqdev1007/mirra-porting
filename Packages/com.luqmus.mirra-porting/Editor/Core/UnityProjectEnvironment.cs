using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;
using PackageSource = UnityEditor.PackageManager.PackageSource;

namespace Luqmus.MirraPorting.Core
{
    /// <summary>
    /// The real project, read once when the object is built, so that a scan works on one
    /// consistent snapshot even if the editor recompiles while it runs.
    /// </summary>
    internal sealed class UnityProjectEnvironment : IProjectEnvironment
    {
        private const string MirraSdkPackageName = "com.romanlee17.mirrasdk5";

        private readonly HashSet<string> _editorSourceFiles;
        private readonly HashSet<string> _playerSourceFiles;
        private readonly List<ScanRoot> _roots;

        internal UnityProjectEnvironment()
        {
            string assetsPath = PathUtil.Normalize(Application.dataPath);
            ProjectRootPath = PathUtil.Normalize(Directory.GetParent(Application.dataPath).FullName);
            ActiveBuildTarget = EditorUserBuildSettings.activeBuildTarget.ToString();
            UnityVersion = Application.unityVersion;
            MirraSdkVersion = VersionOfPackage(MirraSdkPackageName);

            PackageInfo self = PackageInfo.FindForAssembly(typeof(UnityProjectEnvironment).Assembly);
            ToolName = self != null ? self.name : string.Empty;
            ToolVersion = self != null ? self.version : string.Empty;

            _editorSourceFiles = CollectSourceFiles(AssembliesType.Editor);
            _playerSourceFiles = CollectSourceFiles(AssembliesType.Player);
            _roots = CollectRoots(assetsPath);
        }

        public string ProjectRootPath { get; }

        public string ActiveBuildTarget { get; }

        public string UnityVersion { get; }

        public string MirraSdkVersion { get; }

        public string ToolName { get; }

        public string ToolVersion { get; }

        public IReadOnlyList<ScanRoot> GetScanRoots()
        {
            return _roots;
        }

        public bool IsKnownToAnyAssembly(string projectRelativePath)
        {
            return _editorSourceFiles.Contains(projectRelativePath) || _playerSourceFiles.Contains(projectRelativePath);
        }

        public bool IsEditorOnlyAssemblyFile(string projectRelativePath)
        {
            return _editorSourceFiles.Contains(projectRelativePath) && !_playerSourceFiles.Contains(projectRelativePath);
        }

        /// <summary>
        /// Assembly.sourceFiles are project relative with forward slashes, and package files carry
        /// the virtual "Packages/&lt;name&gt;/..." prefix even when the package resolves elsewhere, which
        /// is exactly the shape ScanScope builds. Verified on 2022.3 for embedded, local, git and
        /// registry packages.
        /// </summary>
        private static HashSet<string> CollectSourceFiles(AssembliesType assembliesType)
        {
            var files = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (Assembly assembly in CompilationPipeline.GetAssemblies(assembliesType))
            {
                foreach (string sourceFile in assembly.sourceFiles)
                {
                    files.Add(PathUtil.Normalize(sourceFile));
                }
            }

            return files;
        }

        /// <summary>Version of a registered package, or an empty string when it is not installed.</summary>
        private static string VersionOfPackage(string packageName)
        {
            foreach (PackageInfo package in PackageInfo.GetAllRegisteredPackages())
            {
                if (string.Equals(package.name, packageName, StringComparison.Ordinal))
                {
                    return package.version;
                }
            }

            return string.Empty;
        }

        private static List<ScanRoot> CollectRoots(string assetsPath)
        {
            var roots = new List<ScanRoot> { new ScanRoot("Assets", assetsPath, string.Empty) };

            foreach (PackageInfo package in PackageInfo.GetAllRegisteredPackages())
            {
                // Registry and git packages live in Library/PackageCache and are not the port's own
                // code: nobody is going to edit them, so there is nothing to report there.
                if (package.source != PackageSource.Embedded && package.source != PackageSource.Local)
                {
                    continue;
                }

                roots.Add(new ScanRoot(
                    PathUtil.Normalize(package.assetPath),
                    PathUtil.Normalize(package.resolvedPath),
                    package.name));
            }

            return roots;
        }
    }
}
