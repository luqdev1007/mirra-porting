using System.Collections.Generic;

namespace Luqmus.MirraPorting.Core
{
    /// <summary>
    /// Everything the scan scope needs to know about the project. An interface so that the scope
    /// can be tested on a temporary folder tree instead of on a real Unity project.
    /// </summary>
    internal interface IProjectEnvironment
    {
        /// <summary>Folder that holds Assets and Packages, absolute and normalized.</summary>
        string ProjectRootPath { get; }

        /// <summary>
        /// Active build target, for example "WebGL" or "StandaloneWindows64". Player assemblies
        /// depend on it, so the report has to say which target the editor-only split was made
        /// under.
        /// </summary>
        string ActiveBuildTarget { get; }

        /// <summary>Folders to walk: Assets plus every embedded and local package.</summary>
        IReadOnlyList<ScanRoot> GetScanRoots();

        /// <summary>
        /// The file compiles into some assembly, editor or player. False means no assembly claims
        /// it, so its editor-only state has to be guessed from the path.
        /// </summary>
        /// <param name="projectRelativePath">Path in the shape PathUtil produces.</param>
        bool IsKnownToAnyAssembly(string projectRelativePath);

        /// <summary>The file compiles into an editor assembly and into no player assembly.</summary>
        /// <param name="projectRelativePath">Path in the shape PathUtil produces.</param>
        bool IsEditorOnlyAssemblyFile(string projectRelativePath);
    }
}
