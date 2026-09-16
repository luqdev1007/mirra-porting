using System;
using System.Collections.Generic;

namespace Luqmus.MirraPorting.Core
{
    /// <summary>Collects findings from every check and hands them out in report order.</summary>
    internal sealed class FindingCollector : IFindingSink
    {
        private readonly List<Finding> _findings = new List<Finding>();

        internal int Count
        {
            get { return _findings.Count; }
        }

        public void Add(Finding finding)
        {
            if (finding == null)
            {
                throw new ArgumentNullException(nameof(finding));
            }

            _findings.Add(finding);
        }

        /// <summary>Findings in the order they were added.</summary>
        internal IReadOnlyList<Finding> AsAdded()
        {
            return _findings;
        }

        /// <summary>Findings in report order, see <see cref="FindingOrder"/>.</summary>
        internal IReadOnlyList<Finding> Sorted()
        {
            return FindingOrder.Sort(_findings);
        }
    }
}
