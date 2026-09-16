using System.Collections.Generic;

namespace Luqmus.MirraPorting.Code
{
    /// <summary>
    /// A dotted name as it was written: UnityEngine.Time is ["UnityEngine", "Time"]. What it means
    /// is decided by whoever matches it, not here.
    /// </summary>
    internal sealed class QualifiedName
    {
        internal QualifiedName(IReadOnlyList<string> parts, bool hadGlobalQualifier)
        {
            Parts = parts;
            HadGlobalQualifier = hadGlobalQualifier;
            Dotted = string.Join(".", ToArray(parts));
        }

        internal IReadOnlyList<string> Parts { get; }

        /// <summary>The name was written as global::Something; the qualifier itself is not a part.</summary>
        internal bool HadGlobalQualifier { get; }

        /// <summary>"UnityEngine.Time".</summary>
        internal string Dotted { get; }

        /// <summary>Last part, "Time", or an empty string when there are none.</summary>
        internal string Last
        {
            get { return Parts.Count == 0 ? string.Empty : Parts[Parts.Count - 1]; }
        }

        public override string ToString()
        {
            return HadGlobalQualifier ? "global::" + Dotted : Dotted;
        }

        private static string[] ToArray(IReadOnlyList<string> parts)
        {
            var array = new string[parts.Count];
            for (int i = 0; i < parts.Count; i++)
            {
                array[i] = parts[i];
            }

            return array;
        }
    }
}
