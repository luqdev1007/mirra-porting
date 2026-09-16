namespace Luqmus.MirraPorting.Core
{
    /// <summary>
    /// Finding categories. A category groups rules in the report; it is a plain string so that
    /// later stages can add their own without touching this assembly's public surface.
    /// </summary>
    internal static class Categories
    {
        /// <summary>Failures of the scanner itself.</summary>
        internal const string Scan = "Scan";

        /// <summary>Unity API calls that MirraSDK has to own instead.</summary>
        internal const string ForbiddenApi = "ForbiddenApi";
    }
}
