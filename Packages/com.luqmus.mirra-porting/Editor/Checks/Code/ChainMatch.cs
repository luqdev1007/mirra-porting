using Luqmus.MirraPorting.Code;

namespace Luqmus.MirraPorting.Checks
{
    /// <summary>One place in a file where a forbidden member is touched.</summary>
    internal readonly struct ChainMatch
    {
        internal ChainMatch(
            Token firstToken,
            Token memberToken,
            Token deciderToken,
            ForbiddenMember member,
            AccessKind access,
            TypeNameResolution type)
        {
            FirstToken = firstToken;
            MemberToken = memberToken;
            DeciderToken = deciderToken;
            Member = member;
            Access = access;
            Type = type;
        }

        /// <summary>First token of the chain: global, the namespace, or the type itself.</summary>
        internal Token FirstToken { get; }

        internal Token MemberToken { get; }

        /// <summary>The token that decided the access kind: an assignment, a parenthesis, whatever followed.</summary>
        internal Token DeciderToken { get; }

        internal ForbiddenMember Member { get; }

        internal AccessKind Access { get; }

        internal TypeNameResolution Type { get; }
    }
}
