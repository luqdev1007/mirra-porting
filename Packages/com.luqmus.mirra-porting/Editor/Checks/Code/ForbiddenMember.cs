using System;

namespace Luqmus.MirraPorting.Checks
{
    /// <summary>
    /// One row of the forbidden API table: a member, and which rule to report for each way of
    /// touching it. A missing rule id means that way of touching it is not a finding — reading
    /// Screen.orientation is fine, writing it is not.
    /// </summary>
    internal sealed class ForbiddenMember
    {
        /// <summary>Member name that stands for every member of the type.</summary>
        internal const string AnyMember = "*";

        internal ForbiddenMember(
            string namespaceName,
            string typeName,
            string memberName,
            ApiMemberKind kind,
            string writeRuleId,
            string readRuleId,
            string callRuleId)
        {
            Namespace = namespaceName;
            TypeName = typeName;
            MemberName = memberName;
            Kind = kind;
            WriteRuleId = writeRuleId;
            ReadRuleId = readRuleId;
            CallRuleId = callRuleId;
        }

        /// <summary>"UnityEngine" or "System.IO": the only qualifier that counts as a match.</summary>
        internal string Namespace { get; }

        internal string TypeName { get; }

        internal string MemberName { get; }

        internal ApiMemberKind Kind { get; }

        internal string WriteRuleId { get; }

        internal string ReadRuleId { get; }

        internal string CallRuleId { get; }

        internal bool MatchesAnyMember
        {
            get { return string.Equals(MemberName, AnyMember, StringComparison.Ordinal); }
        }

        /// <summary>Rule for one way of touching the member, or null when that way is allowed.</summary>
        internal string RuleIdFor(AccessKind access)
        {
            switch (access)
            {
                case AccessKind.Write:
                    return WriteRuleId;
                case AccessKind.Call:
                    return CallRuleId;
                default:
                    return ReadRuleId;
            }
        }

        public override string ToString()
        {
            return Namespace + "." + TypeName + "." + MemberName;
        }
    }
}
