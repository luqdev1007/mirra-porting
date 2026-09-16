namespace Luqmus.MirraPorting.Core
{
    /// <summary>
    /// A folder the scanner walks: the Assets folder, or an embedded or local package. The two
    /// paths differ on purpose. A local package lives anywhere on disk, but the project always
    /// addresses it as "Packages/&lt;name&gt;".
    /// </summary>
    internal readonly struct ScanRoot
    {
        internal ScanRoot(string projectRelativePath, string absolutePath, string packageName)
        {
            ProjectRelativePath = projectRelativePath;
            AbsolutePath = absolutePath;
            PackageName = packageName;
        }

        /// <summary>"Assets" or "Packages/&lt;name&gt;", normalized.</summary>
        internal string ProjectRelativePath { get; }

        /// <summary>Where the folder really is on disk, normalized.</summary>
        internal string AbsolutePath { get; }

        /// <summary>Package name, or an empty string for the Assets root.</summary>
        internal string PackageName { get; }
    }
}
