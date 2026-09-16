namespace Luqmus.MirraPorting.Checks
{
    /// <summary>How a member is touched at the place it was found.</summary>
    internal enum AccessKind
    {
        Write = 0,
        Read = 1,
        Call = 2,
    }
}
