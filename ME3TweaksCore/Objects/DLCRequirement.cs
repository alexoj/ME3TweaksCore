using System;
using System.Collections.Generic;
using System.Linq;
using LegendaryExplorerCore.Misc;
using ME3TweaksCore.Helpers;
using ME3TweaksCore.Localization;

namespace ME3TweaksCore.Objects
{
    /// <summary>
    /// Object that specifies a DLC foldername and optionally set of conditions for determining if a DLC meets or does not meet them
    /// </summary>
    public class DLCRequirement : DLCRequirementBase
    {


        /// <summary>
        /// If this uses keyed conditions
        /// </summary>
        public bool IsKeyedVersion { get; set; }

        /// <summary>
        /// Parses out a DLCRequirement string in the form of DLC_MOD_NAME(1.2.3.4) or DLC_MOD_NAME. This is for MODDESC 8.1 AND BELOW!
        /// </summary>
        /// <param name="inputString"></param>
        /// <returns></returns>
        /// <exception cref="Exception">If an error occurs parsing the string</exception>
        public static DLCRequirement ParseRequirement(string inputString, bool supportsVersion, bool canBePlusMinus)
        {
            inputString = inputString.Trim();

            if (!supportsVersion)
                return new DLCRequirement() { DLCFolderName = canBePlusMinus ? new PlusMinusKey(inputString) : new PlusMinusKey(null, inputString) };

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
                return new DLCRequirement() { DLCFolderName = canBePlusMinus ? new PlusMinusKey(inputString) : new PlusMinusKey(null, inputString) };

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

            var fname = inputString.Substring(0, verStart);

            return new DLCRequirement()
            {
                DLCFolderName = canBePlusMinus ? new PlusMinusKey(fname) : new PlusMinusKey(null, fname),
                MinVersion = minVer
            };
        }

        /// <summary>
        /// Parses out a DLCRequirement string in the form of DLC_MOD_NAME[Attr=1, Attr2=Str] or DLC_MOD_NAME
        /// </summary>
        /// <param name="inputString">The input string</param>
        /// <returns></returns>
        /// <exception cref="Exception">If an error occurs parsing the string</exception>
        public static DLCRequirement ParseRequirementKeyed(string input, double featureLevel)
        {
            return new DLCRequirement(input, featureLevel, false); // RequiredDLC does not support +-
        }

        public DLCRequirement() { }

        public DLCRequirement(string input, double featureLevel, bool folderNameIsPlusMinus) : base(input, featureLevel, folderNameIsPlusMinus) { }



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
                return DLCFolderName.ToString();
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
                return DLCFolderName.ToString();
            }
        }

        public override string ToString()
        {
            // This is technically not always correct if it's a single
            return Serialize(false);
        }
    }
}
