using Luqmus.MirraPorting.Core;

namespace Luqmus.MirraPorting.Checks
{
    /// <summary>
    /// Which table type a name in a file stands for, and how sure the check is about it. The
    /// reason travels with the confidence so that the report can explain itself.
    /// </summary>
    internal readonly struct TypeNameResolution
    {
        internal TypeNameResolution(string typeName, Confidence confidence, string reason)
        {
            TypeName = typeName;
            Confidence = confidence;
            Reason = reason;
        }

        internal string TypeName { get; }

        internal Confidence Confidence { get; }

        /// <summary>Why the confidence is not High, in Russian. Empty when it is.</summary>
        internal string Reason { get; }

        /// <summary>The same type, but no lower than the given confidence.</summary>
        internal TypeNameResolution LoweredTo(Confidence confidence, string reason)
        {
            if (confidence <= Confidence)
            {
                return this;
            }

            return new TypeNameResolution(TypeName, confidence, reason);
        }
    }
}
