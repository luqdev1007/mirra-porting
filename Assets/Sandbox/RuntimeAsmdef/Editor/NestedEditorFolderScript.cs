namespace Sandbox.RuntimeAsmdef.Editor
{
    /// <summary>
    /// An Editor folder nested under an asmdef with no platform limits. The asmdef decides, the
    /// folder name does not, so this is player code. Expected EditorOnly: false — the case the
    /// Editor folder fallback would get wrong if the assemblies were not asked first.
    /// </summary>
    public static class NestedEditorFolderScript
    {
    }
}
