using System;
using System.Collections.Generic;
using System.Linq;
using ME3TweaksCore.Diagnostics;
using ME3TweaksCore.Localization;
using ME3TweaksCore.Misc;
using ME3TweaksCore.Targets;

namespace ME3TweaksCore.Objects
{
    /// <summary>
    /// Object that specifies a DLC foldername and a minimum version (as set in MetaCmm)
    /// </summary>
    public class DLCRequirement
    {
        /// <summary>
        /// The DLC foldername to test for
        /// </summary>
        public string DLCFolderName { get; init; }

        /// <summary>
        /// The minimum version for this DLC requirement to be met - CAN BE NULL to specify no min version
        /// </summary>
        public Version MinVersion { get; init; }

        /// <summary>
        /// Tests if this requirement is met against the given target
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public bool IsRequirementMet(GameTarget target, List<string> installedDLC = null)
        {
            installedDLC ??= target.GetInstalledDLC();
            if (installedDLC.Contains(DLCFolderName, StringComparer.InvariantCultureIgnoreCase))
            {
                if (MinVersion == null)
                    return true; // No min version

                var metas = target.GetMetaMappedInstalledDLC(installedDLC: installedDLC);
                // if no metacmm file is found it will be mapped to null. In this case we just set it to version 0 because user did not use M3 and we have no way to determine version
                if (metas.TryGetValue(DLCFolderName, out var meta) && Version.TryParse(meta?.Version ?? @"0.0.0.0", out var version))
                {
                    if (ProperVersion.IsGreaterThanOrEqual(version, MinVersion))
                    {
                        return true;
                    }
                    else
                    {
                        if (meta == null)
                        {
                            MLog.Error($@"DLCRequirement not met: The version of the DLC mod {DLCFolderName} could not be determined; there is no mod manager metadata file with the DLC mod. Manually installed mods are not supported for version checks.");
                        }
                        else
                        {
                            MLog.Information($@"DLCRequirement not met: {version} is less than MinVersion {MinVersion}");
                        }
                    }
                }
            }

            return false;

        }

        /// <summary>
        /// Parses out a DLCRequirement string in the form of DLC_MOD_NAME(1.2.3.4) or DLC_MOD_NAME
        /// </summary>
        /// <param name="inputString"></param>
        /// <returns></returns>
        /// <exception cref="Exception">If an error occurs parsing the string</exception>
        public static DLCRequirement ParseRequirement(string inputString, bool supportsVersion)
        {
            inputString = inputString.Trim();

            if (!supportsVersion)
                return new DLCRequirement() { DLCFolderName = inputString };

            // Mod Manager 8.1 changed from () to [] but we still try to support () for older versions
            // This is due to how string struct parser strips ()
            var verStart = inputString.IndexOf('[');
            var verEnd = inputString.IndexOf(']');

            if (verStart == -1 && verEnd == -1)
            {
                // Try legacy
                verStart = inputString.IndexOf('(');
                verEnd = inputString.IndexOf(')');
            }


            if (verStart == -1 && verEnd == -1)
                return new DLCRequirement() { DLCFolderName = inputString };

            if (verStart == -1 || verEnd == -1 || verStart > verEnd)
            {
                throw new Exception(LC.GetString(LC.string_dlcRequirementInvalidParenthesis));
            }

            string verString = inputString.Substring(verStart + 1, verEnd - (verStart + 1));
            Version minVer;
            if (!Version.TryParse(verString, out minVer))
            {
                throw new Exception(LC.GetString(LC.string_interp_dlcRequirementInvalidBadVersion, verString));
            }

            return new DLCRequirement()
            {
                DLCFolderName = inputString.Substring(0, verStart),
                MinVersion = minVer
            };
        }

        /// <summary>
        /// Serializes this requirement back to the moddesc.ini format
        /// </summary>
        /// <returns>Moddesc.ini format of a DLC requirement</returns>
        public string Serialize(bool isSingle)
        {
            // Singles have a ? prefix
            if (isSingle)
                return $"?{Serialize(false)}"; // do not localize

            if (MinVersion != null)
                // Mod Manager 8.1: We serialize as [] instead of ()
                // Since moddesc editor only supports latest version we use this instead.
                return $@"{DLCFolderName}[{MinVersion}]";
            return DLCFolderName;
        }

        public override string ToString()
        {
            if (MinVersion != null)
                return $@"{DLCFolderName} ({MinVersion})";
            return DLCFolderName;
        }
    }
}
