namespace Luqmus.MirraPorting.Core
{
    /// <summary>Where the scan is, for whoever draws a progress bar.</summary>
    internal readonly struct ScanProgressInfo
    {
        internal ScanProgressInfo(string stage, string detail, float fraction)
        {
            Stage = stage;
            Detail = detail;
            Fraction = fraction;
        }

        /// <summary>What the scan is doing, in Russian.</summary>
        internal string Stage { get; }

        /// <summary>The file or the check it is doing it to.</summary>
        internal string Detail { get; }

        /// <summary>0 to 1.</summary>
        internal float Fraction { get; }
    }

    internal delegate void ScanProgressHandler(ScanProgressInfo progress);

    /// <summary>Asked between files and between checks; true stops the scan.</summary>
    internal delegate bool ScanCancellationCheck();
}
