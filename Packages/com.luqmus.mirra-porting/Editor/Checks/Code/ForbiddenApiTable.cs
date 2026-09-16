using System;
using System.Collections.Generic;

namespace Luqmus.MirraPorting.Checks
{
    /// <summary>
    /// The Unity API MirraSDK has to own, as data. Adding a member is adding a row here and a rule
    /// to the catalog; the check itself does not change.
    /// </summary>
    internal static class ForbiddenApiTable
    {
        private const string UnityEngine = "UnityEngine";
        private const string SystemIo = "System.IO";

        private static readonly ForbiddenMember[] Members =
        {
            Property(UnityEngine, "Time", "timeScale", "API.TIMESCALE_WRITE", "API.TIMESCALE_READ"),

            Property(UnityEngine, "AudioListener", "volume", "API.AUDIO_VOLUME_WRITE", "API.AUDIO_VOLUME_READ"),
            Property(UnityEngine, "AudioListener", "pause", "API.AUDIO_PAUSE_WRITE", "API.AUDIO_PAUSE_READ"),

            Property(UnityEngine, "Cursor", "visible", "API.CURSOR_VISIBLE_WRITE", "API.CURSOR_VISIBLE_READ"),
            Property(UnityEngine, "Cursor", "lockState", "API.CURSOR_LOCK_WRITE", "API.CURSOR_LOCK_READ"),

            // Every PlayerPrefs member is a save that does not travel with the player.
            Method(UnityEngine, "PlayerPrefs", ForbiddenMember.AnyMember, "API.PLAYERPREFS"),

            Method(UnityEngine, "Application", "OpenURL", "API.OPEN_URL"),
            Method(UnityEngine, "Application", "Quit", "API.QUIT"),
            ReadOnlyProperty(UnityEngine, "Application", "systemLanguage", "API.SYSTEM_LANGUAGE"),
            ReadOnlyProperty(UnityEngine, "Application", "isMobilePlatform", "API.IS_MOBILE"),
            ReadOnlyProperty(UnityEngine, "SystemInfo", "deviceType", "API.IS_MOBILE"),
            ReadOnlyProperty(UnityEngine, "Application", "persistentDataPath", "API.PERSISTENT_DATA_PATH"),

            WriteOnlyProperty(UnityEngine, "Application", "runInBackground", "API.RUN_IN_BACKGROUND_WRITE"),

            WriteOnlyProperty(UnityEngine, "Screen", "orientation", "API.ORIENTATION_WRITE"),
            WriteOnlyProperty(UnityEngine, "Screen", "autoRotateToPortrait", "API.ORIENTATION_WRITE"),
            WriteOnlyProperty(UnityEngine, "Screen", "autoRotateToPortraitUpsideDown", "API.ORIENTATION_WRITE"),
            WriteOnlyProperty(UnityEngine, "Screen", "autoRotateToLandscapeLeft", "API.ORIENTATION_WRITE"),
            WriteOnlyProperty(UnityEngine, "Screen", "autoRotateToLandscapeRight", "API.ORIENTATION_WRITE"),

            Method(SystemIo, "File", "WriteAllText", "API.FILE_WRITE"),
            Method(SystemIo, "File", "WriteAllBytes", "API.FILE_WRITE"),
            Method(SystemIo, "File", "WriteAllLines", "API.FILE_WRITE"),
            Method(SystemIo, "File", "AppendAllText", "API.FILE_WRITE"),
            Method(SystemIo, "File", "AppendAllLines", "API.FILE_WRITE"),
        };

        private static readonly Dictionary<string, List<ForbiddenMember>> ByTypeName = BuildIndex(Members);

        internal static IReadOnlyList<ForbiddenMember> All
        {
            get { return Members; }
        }

        /// <summary>Type names the table mentions: Time, Cursor, Application, File and the rest.</summary>
        internal static IReadOnlyCollection<string> TypeNames
        {
            get { return ByTypeName.Keys; }
        }

        internal static bool IsKnownType(string typeName)
        {
            return typeName != null && ByTypeName.ContainsKey(typeName);
        }

        /// <summary>The namespace a type has to be qualified with, or an empty string if unknown.</summary>
        internal static string NamespaceOf(string typeName)
        {
            List<ForbiddenMember> members;
            if (typeName != null && ByTypeName.TryGetValue(typeName, out members))
            {
                return members[0].Namespace;
            }

            return string.Empty;
        }

        /// <summary>
        /// The row for a member: an exact name first, then the row that stands for every member of
        /// the type.
        /// </summary>
        internal static bool TryFind(string typeName, string memberName, out ForbiddenMember member)
        {
            member = null;

            List<ForbiddenMember> members;
            if (typeName == null || memberName == null || !ByTypeName.TryGetValue(typeName, out members))
            {
                return false;
            }

            foreach (ForbiddenMember candidate in members)
            {
                if (string.Equals(candidate.MemberName, memberName, StringComparison.Ordinal))
                {
                    member = candidate;
                    return true;
                }
            }

            foreach (ForbiddenMember candidate in members)
            {
                if (candidate.MatchesAnyMember)
                {
                    member = candidate;
                    return true;
                }
            }

            return false;
        }

        /// <summary>Rows of a type, for matching a bare member brought in by using static.</summary>
        internal static bool TryGetMembers(string typeName, out IReadOnlyList<ForbiddenMember> members)
        {
            List<ForbiddenMember> found;
            if (typeName != null && ByTypeName.TryGetValue(typeName, out found))
            {
                members = found;
                return true;
            }

            members = null;
            return false;
        }

        private static ForbiddenMember Property(
            string namespaceName, string typeName, string memberName, string writeRuleId, string readRuleId)
        {
            return new ForbiddenMember(
                namespaceName, typeName, memberName, ApiMemberKind.Property, writeRuleId, readRuleId, null);
        }

        private static ForbiddenMember ReadOnlyProperty(
            string namespaceName, string typeName, string memberName, string readRuleId)
        {
            return new ForbiddenMember(
                namespaceName, typeName, memberName, ApiMemberKind.Property, null, readRuleId, null);
        }

        private static ForbiddenMember WriteOnlyProperty(
            string namespaceName, string typeName, string memberName, string writeRuleId)
        {
            return new ForbiddenMember(
                namespaceName, typeName, memberName, ApiMemberKind.Property, writeRuleId, null, null);
        }

        /// <summary>A method: naming it without parentheses is a method group, the same finding.</summary>
        private static ForbiddenMember Method(
            string namespaceName, string typeName, string memberName, string ruleId)
        {
            return new ForbiddenMember(
                namespaceName, typeName, memberName, ApiMemberKind.Method, null, ruleId, ruleId);
        }

        private static Dictionary<string, List<ForbiddenMember>> BuildIndex(ForbiddenMember[] members)
        {
            var index = new Dictionary<string, List<ForbiddenMember>>(StringComparer.Ordinal);

            foreach (ForbiddenMember member in members)
            {
                List<ForbiddenMember> list;
                if (!index.TryGetValue(member.TypeName, out list))
                {
                    list = new List<ForbiddenMember>();
                    index.Add(member.TypeName, list);
                }

                list.Add(member);
            }

            return index;
        }
    }
}
