using System;

namespace Luqmus.MirraPorting.Core
{
    /// <summary>
    /// Things the reader has to know before trusting the report. Kept in one place so that the
    /// window and both report formats say them the same way.
    /// </summary>
    internal static class ScanNotices
    {
        /// <summary>The target ports are built for; under any other one the editor-only split shifts.</summary>
        internal const string WebGlTarget = "WebGL";

        internal static bool IsWebGl(string activeBuildTarget)
        {
            return string.Equals(activeBuildTarget, WebGlTarget, StringComparison.Ordinal);
        }

        internal static string NonWebGlPlatform(string activeBuildTarget)
        {
            return "Активная платформа — " + activeBuildTarget + ", а не WebGL. Набор Player-сборок зависит " +
                   "от платформы, поэтому пометка «только редактор» может быть неточной. " +
                   "Перед финальной сверкой переключите платформу на WebGL и пересканируйте.";
        }

        internal static string SdkInsideAssets(string projectRelativePath)
        {
            return "MirraSDK лежит в " + projectRelativePath + ", а не установлен пакетом: версия не определена. " +
                   "Эта папка из сканирования исключена.";
        }
    }
}
