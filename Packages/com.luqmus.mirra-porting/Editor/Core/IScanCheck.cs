namespace Luqmus.MirraPorting.Core
{
    /// <summary>
    /// One check over the scanned project. A check may throw: the runner catches it, reports
    /// SCAN.INTERNAL_ERROR and goes on with the other checks.
    /// </summary>
    internal interface IScanCheck
    {
        /// <summary>Name of the check for SCAN.INTERNAL_ERROR messages and for progress.</summary>
        string Id { get; }

        void Run(ScanContext context, IFindingSink sink);
    }
}
