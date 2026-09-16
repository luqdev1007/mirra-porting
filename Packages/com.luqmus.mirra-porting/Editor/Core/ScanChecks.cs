using System.Collections.Generic;
using Luqmus.MirraPorting.Checks;

namespace Luqmus.MirraPorting.Core
{
    /// <summary>
    /// The checks a scan runs, in one place. Adding a check is adding a line here; the runner does
    /// not change.
    /// </summary>
    internal static class ScanChecks
    {
        internal static IReadOnlyList<IScanCheck> Create()
        {
            return new IScanCheck[]
            {
                new ForbiddenApiCheck(),
            };
        }
    }
}
