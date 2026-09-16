namespace Luqmus.MirraPorting.Core
{
    /// <summary>What the report says about the project it was made from.</summary>
    internal sealed class ProjectInfo
    {
        internal ProjectInfo(
            string path,
            string unityVersion,
            string activeBuildTarget,
            string mirraSdkVersion,
            string toolName,
            string toolVersion)
        {
            Path = path ?? string.Empty;
            UnityVersion = unityVersion ?? string.Empty;
            ActiveBuildTarget = activeBuildTarget ?? string.Empty;
            MirraSdkVersion = mirraSdkVersion ?? string.Empty;
            ToolName = toolName ?? string.Empty;
            ToolVersion = toolVersion ?? string.Empty;
        }

        internal string Path { get; }

        internal string UnityVersion { get; }

        /// <summary>The target the editor-only split was made under.</summary>
        internal string ActiveBuildTarget { get; }

        /// <summary>Empty when MirraSDK is not installed as a package.</summary>
        internal string MirraSdkVersion { get; }

        internal string ToolName { get; }

        internal string ToolVersion { get; }

        internal static ProjectInfo FromEnvironment(IProjectEnvironment environment)
        {
            return new ProjectInfo(
                environment.ProjectRootPath,
                environment.UnityVersion,
                environment.ActiveBuildTarget,
                environment.MirraSdkVersion,
                environment.ToolName,
                environment.ToolVersion);
        }
    }
}
