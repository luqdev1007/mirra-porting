namespace Luqmus.MirraPorting.Core
{
    /// <summary>A C# file the scanner is going to read.</summary>
    internal sealed class ScopedFile
    {
        internal ScopedFile(string projectRelativePath, string absolutePath, bool editorOnly)
        {
            ProjectRelativePath = projectRelativePath;
            AbsolutePath = absolutePath;
            EditorOnly = editorOnly;
        }

        /// <summary>Path as it goes into the report: "Assets/..." or "Packages/&lt;name&gt;/...".</summary>
        internal string ProjectRelativePath { get; }

        /// <summary>Path to read the file with.</summary>
        internal string AbsolutePath { get; }

        /// <summary>
        /// The whole file is editor only: it compiles into an editor assembly and into no player
        /// assembly. Code guarded by #if UNITY_EDITOR inside an ordinary file is decided per token
        /// in step 4, not here.
        /// </summary>
        internal bool EditorOnly { get; }
    }
}
