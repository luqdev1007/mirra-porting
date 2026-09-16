using System;
using System.Collections.Generic;
using Luqmus.MirraPorting.Code;
using Luqmus.MirraPorting.Core;

namespace Luqmus.MirraPorting.Checks
{
    /// <summary>
    /// What the names of the forbidden API table mean inside one file: which of them are usable at
    /// all, how sure a bare use of each is, and which members a using static brought in.
    ///
    /// Built once per file, so matching is a dictionary lookup per token.
    /// </summary>
    internal sealed class FileTypeIndex
    {
        private const string GlobalNamespaceReason = "глобальный namespace";

        private readonly Dictionary<string, TypeNameResolution> _bareTypes;
        private readonly HashSet<string> _blocked;
        private readonly Dictionary<string, ForbiddenMember> _staticMembers;
        private readonly Dictionary<string, string> _staticMemberReasons;

        private FileTypeIndex(
            Dictionary<string, TypeNameResolution> bareTypes,
            HashSet<string> blocked,
            Dictionary<string, ForbiddenMember> staticMembers,
            Dictionary<string, string> staticMemberReasons)
        {
            _bareTypes = bareTypes;
            _blocked = blocked;
            _staticMembers = staticMembers;
            _staticMemberReasons = staticMemberReasons;
        }

        /// <summary>Nothing in this file can match: the token loop can be skipped.</summary>
        internal bool IsEmpty
        {
            get { return _bareTypes.Count == 0 && _staticMembers.Count == 0; }
        }

        internal static FileTypeIndex Build(SourceFile file, DeclaredTypeIndex declaredTypes)
        {
            var bareTypes = new Dictionary<string, TypeNameResolution>(StringComparer.Ordinal);
            var blocked = new HashSet<string>(StringComparer.Ordinal);
            var staticMembers = new Dictionary<string, ForbiddenMember>(StringComparer.Ordinal);
            var staticMemberReasons = new Dictionary<string, string>(StringComparer.Ordinal);

            foreach (string typeName in ForbiddenApiTable.TypeNames)
            {
                bareTypes.Add(typeName, ResolveBareUse(typeName, file, declaredTypes));
            }

            // An alias renames things: it can bring a type in under a new name, and it can take a
            // name away from the table when it points somewhere else entirely.
            foreach (KeyValuePair<string, QualifiedName> alias in file.Usings.Aliases)
            {
                TypeNameResolution target;
                if (TryResolveTarget(alias.Value, out target))
                {
                    bareTypes[alias.Key] = target;
                    blocked.Remove(alias.Key);
                }
                else
                {
                    bareTypes.Remove(alias.Key);
                    blocked.Add(alias.Key);
                }
            }

            foreach (QualifiedName staticUsing in file.Usings.StaticUsings)
            {
                TypeNameResolution target;
                if (!TryResolveTarget(staticUsing, out target))
                {
                    continue;
                }

                IReadOnlyList<ForbiddenMember> members;
                if (!ForbiddenApiTable.TryGetMembers(target.TypeName, out members))
                {
                    continue;
                }

                string reason = "через using static " +
                                ForbiddenApiTable.NamespaceOf(target.TypeName) + "." + target.TypeName;

                foreach (ForbiddenMember member in members)
                {
                    // A row that stands for every member would match every identifier in the file.
                    if (member.MatchesAnyMember)
                    {
                        continue;
                    }

                    staticMembers[member.MemberName] = member;
                    staticMemberReasons[member.MemberName] = reason;
                }
            }

            return new FileTypeIndex(bareTypes, blocked, staticMembers, staticMemberReasons);
        }

        /// <summary>A name written on its own, for example Time in Time.timeScale.</summary>
        internal bool TryResolveBareType(string identifier, out TypeNameResolution resolution)
        {
            resolution = default(TypeNameResolution);
            if (identifier == null || _blocked.Contains(identifier))
            {
                return false;
            }

            return _bareTypes.TryGetValue(identifier, out resolution);
        }

        /// <summary>
        /// A name behind a qualifier. Only the type's own namespace counts, so MyGame.Time and
        /// x.Time are not matches, and UnityEngine.Time is one whatever the project declares.
        /// </summary>
        internal bool TryResolveQualifiedType(string qualifier, string identifier, out TypeNameResolution resolution)
        {
            resolution = default(TypeNameResolution);
            if (qualifier == null || identifier == null || !ForbiddenApiTable.IsKnownType(identifier))
            {
                return false;
            }

            if (!string.Equals(qualifier, ForbiddenApiTable.NamespaceOf(identifier), StringComparison.Ordinal))
            {
                return false;
            }

            resolution = new TypeNameResolution(identifier, Confidence.High, string.Empty);
            return true;
        }

        /// <summary>A member name on its own, brought into the file by using static.</summary>
        internal bool TryResolveStaticMember(string identifier, out ForbiddenMember member, out TypeNameResolution type)
        {
            type = default(TypeNameResolution);
            member = null;

            if (identifier == null || !_staticMembers.TryGetValue(identifier, out member))
            {
                return false;
            }

            // Always low: a bare name is the weakest evidence there is.
            type = new TypeNameResolution(member.TypeName, Confidence.Low, _staticMemberReasons[identifier]);
            return true;
        }

        /// <summary>
        /// How sure a bare use of a table type is. Missing using, a project type of the same name
        /// visible here — the lowest reason wins.
        /// </summary>
        private static TypeNameResolution ResolveBareUse(
            string typeName, SourceFile file, DeclaredTypeIndex declaredTypes)
        {
            var resolution = new TypeNameResolution(typeName, Confidence.High, string.Empty);

            string namespaceName = ForbiddenApiTable.NamespaceOf(typeName);
            if (!file.Usings.UsesNamespace(namespaceName))
            {
                resolution = string.Equals(namespaceName, "UnityEngine", StringComparison.Ordinal)
                    ? resolution.LoweredTo(Confidence.Medium, "в файле нет using UnityEngine")
                    : resolution.LoweredTo(Confidence.Low, typeName + " без using " + namespaceName);
            }

            string shadowingNamespace;
            if (IsShadowed(typeName, file, declaredTypes, out shadowingNamespace))
            {
                string where = shadowingNamespace.Length == 0 ? GlobalNamespaceReason : shadowingNamespace;
                resolution = resolution.LoweredTo(
                    Confidence.Low, "в проекте объявлен свой тип " + typeName + " (" + where + ")");
            }

            return resolution;
        }

        /// <summary>
        /// A project type shadows a Unity type only where it is visible: in the global namespace,
        /// in a namespace the file imports, or in a namespace the file itself is inside.
        /// </summary>
        private static bool IsShadowed(
            string typeName, SourceFile file, DeclaredTypeIndex declaredTypes, out string shadowingNamespace)
        {
            shadowingNamespace = string.Empty;

            IReadOnlyList<string> namespaces;
            if (declaredTypes == null || !declaredTypes.TryGetNamespaces(typeName, out namespaces))
            {
                return false;
            }

            foreach (string declaredIn in namespaces)
            {
                if (declaredIn.Length == 0 || file.Usings.UsesNamespace(declaredIn) || IsInsideOrUnder(file, declaredIn))
                {
                    shadowingNamespace = declaredIn;
                    return true;
                }
            }

            return false;
        }

        /// <summary>A file in namespace A.B sees declarations of A.B and of A.</summary>
        private static bool IsInsideOrUnder(SourceFile file, string declaredIn)
        {
            foreach (string fileNamespace in file.Declarations.Namespaces)
            {
                if (string.Equals(fileNamespace, declaredIn, StringComparison.Ordinal) ||
                    fileNamespace.StartsWith(declaredIn + ".", StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// What an alias or a using static points at. The qualifier has to be the type's own
        /// namespace, so using F = System.IO.File works the same way as using T = UnityEngine.Time.
        /// </summary>
        private static bool TryResolveTarget(QualifiedName name, out TypeNameResolution resolution)
        {
            resolution = default(TypeNameResolution);
            if (name == null || name.Parts.Count == 0)
            {
                return false;
            }

            string typeName = name.Last;
            if (!ForbiddenApiTable.IsKnownType(typeName))
            {
                return false;
            }

            if (name.Parts.Count == 1)
            {
                resolution = new TypeNameResolution(typeName, Confidence.Medium, "алиас без UnityEngine");
                return true;
            }

            var qualifier = new System.Text.StringBuilder();
            for (int i = 0; i < name.Parts.Count - 1; i++)
            {
                if (i > 0)
                {
                    qualifier.Append('.');
                }

                qualifier.Append(name.Parts[i]);
            }

            if (!string.Equals(qualifier.ToString(), ForbiddenApiTable.NamespaceOf(typeName), StringComparison.Ordinal))
            {
                return false;
            }

            resolution = new TypeNameResolution(typeName, Confidence.High, string.Empty);
            return true;
        }
    }
}
