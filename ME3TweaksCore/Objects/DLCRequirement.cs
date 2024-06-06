using System;
using System.Collections.Generic;
using System.Linq;
using LegendaryExplorerCore.Misc;
using ME3TweaksCore.Diagnostics;
using ME3TweaksCore.GameFilesystem;
using ME3TweaksCore.Helpers;
using ME3TweaksCore.Localization;
using ME3TweaksCore.Misc;
using ME3TweaksCore.Targets;

namespace ME3TweaksCore.Objects
{
    /// <summary>
    /// Object that specifies a DLC foldername and optionally set of conditions for determining if a DLC meets or does not meet them
    /// </summary>
    public class DLCRequirement
    {
        private const string REQKEY_MINVERSION = @"minversion";
        private const string REQKEY_DLCOPTIONKEY = @"optionkey";

        /// <summary>
        /// The DLC foldername to test for
        /// </summary>
        public string DLCFolderName { get; init; }

        /// <summary>
        /// The minimum version for this DLC requirement to be met - CAN BE NULL to specify no min version
        /// </summary>
        public Version MinVersion { get; init; }

        /// <summary>
        /// Chosen option requirements for DLC - CAN BE NULL to specify no options 
        /// </summary>
        public List<PlusMinusKey> DLCOptionKeys { get; init; }

        /// <summary>
        /// If this uses keyed conditions
        /// </summary>
        public bool IsKeyedVersion { get; set; }

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
                // Check if there is anything beyond DLC present
                if (!HasConditions())
                    return true; // No min version

                var metas = target.GetMetaMappedInstalledDLC(installedDLC: installedDLC);

                // if no metacmm file is found it will be mapped to null.
                if (metas.TryGetValue(DLCFolderName, out var meta) && meta != null)
                {
                    // MINIMUM VERSION
                    if (MinVersion != null)
                    {
                        if (Version.TryParse(meta.Version, out var version))
                        {
                            if (!ProperVersion.IsLessThan(version, MinVersion))
                            {
                                // Does not meet minimum version requirement
                                MLog.Error($@"DLCRequirement not met: {version} is less than MinVersion {MinVersion}");
                                return false;
                            }

                            // Version DLC Requirement Met
                        }
                        else
                        {
                            MLog.Error($@"DLCRequirement not met: Could not read a semantic mod version of {DLCFolderName} from its _metacmm file. Cannot conduct minimum version requirement. Value: {meta.Version}");
                            return false;
                        }
                    }

                    // OPTION KEYS
                    if (DLCOptionKeys != null)
                    {
                        foreach (var key in DLCOptionKeys)
                        {
                            if (!IsDLCOptionKeyRequirementMet(meta, key))
                            {
                                MLog.Error($@"DLCRequirement not met: DLC Option Key requirement is not met: {DLCFolderName} is not installed with optionkey value specified: {key}");
                                return false;
                            }
                        }
                    }
                }
                else
                {
                    MLog.Error($@"DLCRequirement not met: Information about the mod of the DLC mod {DLCFolderName} could not be determined; there is no mod manager metadata file with the DLC mod. Manually installed mods are not supported for extended DLC requirements.");
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Checks if this alternate meets the conditional DLC requirements for option keys in other mods.
        /// </summary>
        /// <param name="metaInfo"></param>
        /// <returns></returns>
        public bool IsDLCOptionKeyRequirementMet(MetaCMM meta, PlusMinusKey okd)
        {
            if (okd.IsPlus == true && !meta.OptionKeysSelectedAtInstallTime.Contains(okd.Key, StringComparer.InvariantCultureIgnoreCase))
            {
                // Option that must have been chosen was not.
                return false;
            }
            if (okd.IsPlus == false && meta.OptionKeysSelectedAtInstallTime.Contains(okd.Key, StringComparer.InvariantCultureIgnoreCase))
            {
                // Option that must not have been chosen in another mod was chosen.
                return false;
            }

            return true;
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
                // Todo: UPDATE LOCALIZATION FOR THIS
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
        /// Parses out a DLCRequirement string in the form of DLC_MOD_NAME[Attr=1, Attr2=Str] or DLC_MOD_NAME
        /// </summary>
        /// <param name="inputString">The input string</param>
        /// <returns></returns>
        /// <exception cref="Exception">If an error occurs parsing the string</exception>
        public static DLCRequirement ParseRequirementKeyed(string inputString)
        {
            inputString = inputString.Trim();

            if (inputString.Contains('(') || inputString.Contains(')'))
            {
                throw new Exception("DLCRequirements cannot contain ( or ) due to parser issues that would potentially break backwards compatibility if fixed. Use [ ] instead for conditions.");
            }

            var structStart = inputString.IndexOf('[');
            var structEnd = inputString.IndexOf(']');

            // No conditions.
            if (structStart == -1 && structEnd == -1)
                return new DLCRequirement() { DLCFolderName = inputString };

            // Backwards ][.
            if (structStart == -1 || structEnd == -1 || structStart > structEnd)
            {
                // Todo: UPDATE LOCALIZATION FOR THIS
                throw new Exception(LC.GetString(LC.string_dlcRequirementInvalidParenthesis));
            }

            string attributeStruct = inputString.Substring(structStart + 1, structEnd - (structStart + 1));
            var keyMap = StringStructParser.GetSplitMultiValues(attributeStruct, canBeCaseInsensitive: true);
            if (keyMap.Count == 0)
                throw new Exception($"Conditions in DLCRequirement must be in brackets, comma separated, and use key=value format. Bad value: {inputString}");

            // Defaults
            Version minVer = null;
            List<PlusMinusKey> dlcOptionKeys = null;


            // Read values
            foreach (var key in keyMap)
            {
                // Conditions.
                switch (key.Key)
                {
                    case DLCRequirement.REQKEY_MINVERSION:
                        if (!Version.TryParse(key.Value[0], out minVer))
                        {
                            throw new Exception(LC.GetString(LC.string_interp_dlcRequirementInvalidBadVersion, key.Value));
                        }
                        break;
                    case DLCRequirement.REQKEY_DLCOPTIONKEY:
                        foreach (var value in key.Value)
                        {
                            var reqKey = new PlusMinusKey(value);
                            if (reqKey.IsPlus == null)
                            {
                                throw new Exception($"Invalid DLCRequirement: DLCOptionKeys values must be prefixed with + or -. Bad value: {value}");
                            }

                            dlcOptionKeys ??= new List<PlusMinusKey>();
                            dlcOptionKeys.Add(reqKey);
                        }

                        break;
                    default:
                        throw new Exception($"Unknown descriptor in DLCRequirement: {key.Key}");
                }
            }


            return new DLCRequirement()
            {
                IsKeyedVersion = true,
                DLCFolderName = inputString.Substring(0, structStart),
                MinVersion = minVer,
                DLCOptionKeys = dlcOptionKeys
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

            if (IsKeyedVersion)
            {
                if (HasConditions())
                {
                    CaseInsensitiveDictionary<List<string>> keyMap = new CaseInsensitiveDictionary<List<string>>();
                    if (MinVersion != null)
                        keyMap[REQKEY_MINVERSION] = [MinVersion.ToString()];
                    if (DLCOptionKeys != null)
                        keyMap[REQKEY_DLCOPTIONKEY] = DLCOptionKeys.Select(x => x.ToString()).ToList();

                    return $"{DLCFolderName}{StringStructParser.BuildCommaSeparatedSplitMultiValueList(keyMap, '[', ']')}";
                }

                // No Conditions.
                return DLCFolderName;
            }
            else
            {
                // Pre Mod Manager 9
                if (MinVersion != null)
                {
                    // Mod Manager 8.1: We serialize as [] instead of ()
                    // Since moddesc editor only supports latest version we use this instead.
                    return $@"{DLCFolderName}[{MinVersion}]";
                }

                // No conditions.
                return DLCFolderName;
            }
        }

        private bool HasConditions()
        {
            return DLCOptionKeys != null || MinVersion != null;
        }

        public override string ToString()
        {
            // This is technically not always correct if it's a single
            return Serialize(false);
        }
    }
}
