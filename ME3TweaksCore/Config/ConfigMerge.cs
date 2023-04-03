using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LegendaryExplorerCore.Coalesced;
using LegendaryExplorerCore.Misc;
using LegendaryExplorerCore.Packages;
using ME3TweaksCore.Diagnostics;

namespace ME3TweaksCore.Config
{
    /// <summary>
    /// Class for handling merging a Coalesced delta to a file
    /// </summary>
    public class ConfigMerge
    {

#if DEBUG
        private static readonly bool DebugConfigMerge = true;
#else
        private static readonly bool DebugConfigMerge = false;
#endif
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
        public static void PerformMerge(CaseInsensitiveDictionary<CoalesceAsset> assets, CoalesceAsset delta, MEGame game)
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
                        MergeEntry(inisection, entry, game);
                    }
                }
            }
        }

        private static void MergeEntry(CoalesceSection value, CoalesceProperty property, MEGame game)
        {
            foreach (var prop in property)
            {
                switch (prop.ParseAction)
                {
                    case CoalesceParseAction.New: // Type 0 - Overwrite or add
                        {
                            if (value.TryGetValue(property.Name, out var values))
                            {
                                values.Clear(); // Remove all existing values on this property.
                                MLog.Debug($@"ConfigMerge::MergeEntry - Setting value {property.Name}->{prop.Value}", shouldLog: DebugConfigMerge);
                                values.Add(new CoalesceValue(prop.Value, game == MEGame.LE1 ? CoalesceParseAction.Add : prop.ParseAction)); // Add our entry to this property.
                                continue;
                            }

                            // We are just adding the new property itself.
                            // Todo: Double check if we need double typing (++/--/..) for LE2/LE3 so you can run it on existing stuff as well as basedon stuff
                            MLog.Debug($@"ConfigMerge::MergeEntry - Setting NEW value {property.Name}->{prop.Value}", shouldLog: DebugConfigMerge);
                            value.AddEntry(new CoalesceProperty(property.Name, new CoalesceValue(prop.Value, game == MEGame.LE1 ? CoalesceParseAction.Add : prop.ParseAction))); // Add our property to the list
                        }
                        break;
                    case CoalesceParseAction.Add: // Type 2 - add
                        MLog.Debug($@"ConfigMerge::MergeEntry - Adding value {property.Name}->{prop.Value}", shouldLog: DebugConfigMerge);
                        value.AddEntry(new CoalesceProperty(property.Name, prop.Value)); // Add our property to the list
                        break;
                    case CoalesceParseAction.AddUnique:
                        {
                            if (value.TryGetValue(property.Name, out var values))
                            {
                                for (int i = values.Count - 1; i >= 0; i--)
                                {
                                    if (values[i].Value == prop.Value)
                                    {
                                        MLog.Debug($@"ConfigMerge::MergeEntry - Not adding duplicate value {property.Name}->{prop.Value} on {value.Name}",
                                            shouldLog: DebugConfigMerge);
                                        continue;
                                    }
                                }
                            }
                            // It's new just add the whole thing or did not find existing one
                            // Todo: LE1 only supports type 2
                            // Todo: Double check if we need double typing (++/--/..) for LE2/LE3 so you can run it on existing stuff as well as basedon stuff
                            value.AddEntry(new CoalesceProperty(property.Name, new CoalesceValue(prop.Value, game == MEGame.LE1 ? CoalesceParseAction.Add : prop.ParseAction))); // Add our property to the list
                        }
                        break;
                    case CoalesceParseAction.RemoveProperty:
                        value.RemoveAllNamedEntries(property.Name);
                        break;
                    case CoalesceParseAction.Remove:
                        {
                            if (value.TryGetValue(property.Name, out var values))
                            {
                                for (int i = values.Count - 1; i >= 0; i--)
                                {
                                    if (values[i].Value == prop.Value)
                                    {
                                        MLog.Debug(
                                            $@"ConfigMerge::MergeEntry - Removing value {property.Name}->{prop.Value} from {value.Name}",
                                            shouldLog: DebugConfigMerge);
                                        values.RemoveAt(i); // Remove this value
                                    }
                                }
                            }
                        }
                        break;
                    default:
                        Debug.WriteLine($"TYPE NOT IMPLEMENTED: {prop.ParseAction}");
                        break;
                }
            }
        }

        public static void PerformDLCMerge(string getDlcPath, string dlcFolderInstalled)
        {
            throw new NotImplementedException();
        }
    }
}
