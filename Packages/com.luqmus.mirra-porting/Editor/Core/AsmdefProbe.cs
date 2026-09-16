using System;
using System.IO;
using UnityEngine;

namespace Luqmus.MirraPorting.Core
{
    /// <summary>
    /// Reads the assembly name out of an .asmdef file. Used to find a copy of MirraSDK that lives
    /// under Assets instead of in a package: such a folder is excluded from the scan.
    /// </summary>
    internal static class AsmdefProbe
    {
        private const string SdkAssemblyPrefix = "MirraGames.SDK";

        /// <summary>Assembly name from an .asmdef file, or an empty string if it cannot be read.</summary>
        internal static string ReadAssemblyName(string asmdefFilePath)
        {
            string json;
            try
            {
                json = File.ReadAllText(asmdefFilePath);
            }
            catch (Exception)
            {
                // An unreadable asmdef must not stop the scan: the folder is simply not excluded.
                return string.Empty;
            }

            return ReadAssemblyNameFromJson(json);
        }

        /// <summary>Assembly name from .asmdef contents, or an empty string if it cannot be read.</summary>
        internal static string ReadAssemblyNameFromJson(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return string.Empty;
            }

            try
            {
                AsmdefFields fields = JsonUtility.FromJson<AsmdefFields>(json);
                if (fields == null || string.IsNullOrEmpty(fields.name))
                {
                    return string.Empty;
                }

                return fields.name;
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        /// <summary>True for the MirraSDK assemblies listed in CLAUDE.md, all of which share a prefix.</summary>
        internal static bool IsSdkAssemblyName(string assemblyName)
        {
            if (string.IsNullOrEmpty(assemblyName))
            {
                return false;
            }

            return assemblyName.StartsWith(SdkAssemblyPrefix, StringComparison.Ordinal);
        }

        /// <summary>
        /// The one field of an .asmdef the scanner cares about. JsonUtility ignores the rest.
        /// </summary>
        [Serializable]
        private sealed class AsmdefFields
        {
#pragma warning disable 0649 // Assigned by JsonUtility.
            public string name;
#pragma warning restore 0649
        }
    }
}
