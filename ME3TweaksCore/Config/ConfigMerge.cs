using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LegendaryExplorerCore.Coalesced;
using LegendaryExplorerCore.Misc;

namespace ME3TweaksCore.Config
{
    /// <summary>
    /// Class for handling merging a Coalesced delta to a file
    /// </summary>
    public class ConfigMerge
    {
        /// <summary>
        /// Splits a delta section name into filename and actual section name in the config file.
        /// </summary>
        /// <param name="deltaSectionName"></param>
        /// <param name="iniSectionName"></param>
        /// <returns></returns>
        private static string GetConfigFileData(string deltaSectionName, out string iniSectionName)
        {
            var result = deltaSectionName.Substring(0, deltaSectionName.IndexOf(@" "));
            iniSectionName = deltaSectionName.Substring(result.Length + 1); // The rest of the string
            return result;
        }

        /// <summary>
        /// Merges the source file t
        /// </summary>
        /// <param name="sourceFileText"></param>
        /// <param name="deltaText"></param>
        /// <returns></returns>
        public static void PerformMerge(CaseInsensitiveDictionary<CoalesceAsset> assets, CoalesceAsset delta)
        {
            foreach (var section in delta.Sections)
            {
                var iniFilename = GetConfigFileData(section.Key, out var iniSectionName);
                if (assets.TryGetValue(iniFilename, out var ini))
                {
                    // We have the ini file, now we need the section...

                    var inisection = ini.GetOrAddSection(iniSectionName);
                    foreach (var entry in section.Value.Values)
                    {
                        MergeEntry(inisection, entry);
                    }
                }
            }
        }

        private static void MergeEntry(CoalesceSection value, CoalesceProperty property)
        {
            foreach (var prop in property)
            {
                switch (prop.ParseAction)
                {
                    case CoalesceParseAction.Add:
                        value.AddEntry(new CoalesceProperty(property.Name, prop.Value)); // Add our property to the list
                        break;
                    default:
                        Debug.WriteLine($"TYPE NOT IMPLEMENTED: {prop.ParseAction}");
                        break;
                }
            }
        }
    }
}
