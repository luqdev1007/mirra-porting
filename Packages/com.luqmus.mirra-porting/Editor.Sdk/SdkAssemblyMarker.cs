namespace Luqmus.MirraPorting.Sdk
{
    /// <summary>
    /// Compile-time proof that this assembly links against MirraSDK5 when the SDK is installed.
    /// The field is internal, not private, because an unused private field raises CS0414.
    /// </summary>
    internal static class SdkAssemblyMarker
    {
        internal static readonly System.Type SdkEntryPoint = typeof(MirraGames.SDK.MirraSDK);
    }
}
