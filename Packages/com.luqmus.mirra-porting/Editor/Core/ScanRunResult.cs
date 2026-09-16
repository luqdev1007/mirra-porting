namespace Luqmus.MirraPorting.Core
{
    /// <summary>
    /// What a run produced. A cancelled run has no report: nothing is written and the previous one
    /// stays where it was.
    /// </summary>
    internal sealed class ScanRunResult
    {
        internal static readonly ScanRunResult Cancelled = new ScanRunResult(true, null);

        private ScanRunResult(bool cancelled, ScanReport report)
        {
            WasCancelled = cancelled;
            Report = report;
        }

        internal bool WasCancelled { get; }

        /// <summary>Null when the run was cancelled.</summary>
        internal ScanReport Report { get; }

        internal static ScanRunResult Completed(ScanReport report)
        {
            return new ScanRunResult(false, report);
        }
    }
}
