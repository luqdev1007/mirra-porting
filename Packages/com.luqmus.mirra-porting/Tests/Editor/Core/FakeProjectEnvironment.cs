using System;
using System.Collections.Generic;
using Luqmus.MirraPorting.Core;

namespace Luqmus.MirraPorting.Tests.Core
{
    /// <summary>
    /// A project made of a temporary folder tree: lets the scope be tested without an open Unity
    /// project and without recompiling assemblies to change what is editor only.
    /// </summary>
    internal sealed class FakeProjectEnvironment : IProjectEnvironment
    {
        private readonly List<ScanRoot> _roots = new List<ScanRoot>();
        private readonly HashSet<string> _editorFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _playerFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        internal FakeProjectEnvironment(string projectRootPath)
        {
            ProjectRootPath = PathUtil.Normalize(projectRootPath);
            ActiveBuildTarget = "WebGL";
            UnityVersion = "2022.3.62f1";
            MirraSdkVersion = string.Empty;
            ToolName = "com.luqmus.mirra-porting";
            ToolVersion = "0.1.0";
        }

        public string ProjectRootPath { get; }

        public string ActiveBuildTarget { get; set; }

        public string UnityVersion { get; set; }

        public string MirraSdkVersion { get; set; }

        public string ToolName { get; set; }

        public string ToolVersion { get; set; }

        public IReadOnlyList<ScanRoot> GetScanRoots()
        {
            return _roots;
        }

        public bool IsKnownToAnyAssembly(string projectRelativePath)
        {
            return _editorFiles.Contains(projectRelativePath) || _playerFiles.Contains(projectRelativePath);
        }

        public bool IsEditorOnlyAssemblyFile(string projectRelativePath)
        {
            return _editorFiles.Contains(projectRelativePath) && !_playerFiles.Contains(projectRelativePath);
        }

        internal void AddRoot(string projectRelativePath, string absolutePath, string packageName)
        {
            _roots.Add(new ScanRoot(
                PathUtil.Normalize(projectRelativePath), PathUtil.Normalize(absolutePath), packageName));
        }

        internal void InEditorAssembly(string projectRelativePath)
        {
            _editorFiles.Add(projectRelativePath);
        }

        internal void InPlayerAssembly(string projectRelativePath)
        {
            _playerFiles.Add(projectRelativePath);
        }
    }
}
