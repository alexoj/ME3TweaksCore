using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LegendaryExplorerCore.Coalesced;
using LegendaryExplorerCore.Coalesced.Xml;
using LegendaryExplorerCore.Helpers;
using LegendaryExplorerCore.Misc;
using LegendaryExplorerCore.Packages;
using ME3TweaksCore.Diagnostics;
using ME3TweaksCore.GameFilesystem;

namespace ME3TweaksCore.Config
{
    /// <summary>
    /// Class for handling merging config deltas
    /// </summary>
    public class ConfigMerge
    {
        public const string CONFIG_MERGE_PREFIX = @"ConfigDelta-";
        public const string CONFIG_MERGE_EXTENSION = @".m3cd";

#if DEBUG
        private static readonly bool DebugConfigMerge = false;
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
        /// Merges the delta into the asset bundle
        /// </summary>
        /// <param name="assetBundle">Bundle to merge into</param>
        /// <param name="delta">Delta of changes to apply</param>
        /// <param name="game"></param>
        public static void PerformMerge(ConfigAssetBundle assetBundle, CoalesceAsset delta)
        {
            foreach (var section in delta.Sections)
            {
                var iniFilename = GetConfigFileData(section.Key, out var iniSectionName);
                var asset = assetBundle.GetAsset(iniFilename);

                // We have the ini file, now we need the section...
                var inisection = asset.GetOrAddSection(iniSectionName);
                foreach (var entry in section.Value.Values)
                {
                    assetBundle.HasChanges |= MergeEntry(inisection, entry, assetBundle.Game);
                }
            }
        }

        /// <summary>
        /// Merges the incoming property into the target section
        /// </summary>
        /// <param name="targetSection"></param>
        /// <param name="incomingProperty"></param>
        /// <param name="game"></param>
        /// <returns></returns>
        internal static bool MergeEntry(CoalesceSection targetSection, CoalesceProperty incomingProperty, MEGame game)
        {
            bool hasChanged = false;

            // Check if this is a double typed property. If it is, we should process it as an addition rather 
            // than as a merge.
            if (applyDoubleTypedItem(targetSection, incomingProperty))
            {
                return true;
            }

            foreach (var prop in incomingProperty)
            {
                switch (prop.ParseAction)
                {
                    case CoalesceParseAction.New: // Type 0 - Overwrite or add
                        {
                            if (targetSection.TryGetValue(incomingProperty.Name, out var values))
                            {
                                values.Clear(); // Remove all existing values on this property.
                                MLog.Debug($@"ConfigMerge::MergeEntry - Setting value {incomingProperty.Name}->{prop.Value} in {targetSection.Name}", shouldLog: DebugConfigMerge);
                                values.Add(new CoalesceValue(prop.Value, game == MEGame.LE1 ? CoalesceParseAction.Add : prop.ParseAction)); // Add our entry to this property.
                                continue;
                            }

                            // We are just adding the new property itself.
                            // Todo: Double check if we need double typing (++/--/..) for LE2/LE3 so you can run it on existing stuff as well as basedon stuff
                            MLog.Debug($@"ConfigMerge::MergeEntry - Setting NEW value {incomingProperty.Name}->{prop.Value}", shouldLog: DebugConfigMerge);
                            targetSection.AddEntry(new CoalesceProperty(incomingProperty.Name, new CoalesceValue(prop.Value, game == MEGame.LE1 ? CoalesceParseAction.Add : prop.ParseAction))); // Add our property to the list
                            hasChanged = true;
                        }
                        break;
                    case CoalesceParseAction.Add: // Type 2 - add
                        MLog.Debug($@"ConfigMerge::MergeEntry - Adding value {incomingProperty.Name}->{prop.Value} to {targetSection.Name}", shouldLog: DebugConfigMerge);
                        targetSection.AddEntry(new CoalesceProperty(incomingProperty.Name, prop.Value)); // Add our property to the list
                        hasChanged = true;
                        break;
                    case CoalesceParseAction.AddUnique:
                        {
                            if (targetSection.TryGetValue(incomingProperty.Name, out var values))
                            {
                                for (int i = values.Count - 1; i >= 0; i--)
                                {
                                    if (values[i].Value == prop.Value)
                                    {
                                        MLog.Debug($@"ConfigMerge::MergeEntry - Not adding duplicate value {incomingProperty.Name}->{prop.Value} on {targetSection.Name}",
                                            shouldLog: DebugConfigMerge);
                                        continue;
                                    }
                                }
                            }
                            // It's new just add the whole thing or did not find existing one
                            // Todo: LE1 only supports type 2
                            // Todo: Double check if we need double typing (++/--/..) for LE2/LE3 so you can run it on existing stuff as well as basedon stuff
                            MLog.Debug($@"ConfigMerge::MergeEntry - Adding unique value {incomingProperty.Name}->{prop.Value} to {targetSection.Name}",
                                shouldLog: DebugConfigMerge);
                            targetSection.AddEntry(new CoalesceProperty(incomingProperty.Name, new CoalesceValue(prop.Value, game == MEGame.LE1 ? CoalesceParseAction.Add : prop.ParseAction))); // Add our property to the list
                            hasChanged = true;

                        }
                        break;
                    case CoalesceParseAction.RemoveProperty: // Type 1
                        MLog.Debug($@"ConfigMerge::MergeEntry - Removing entire property {incomingProperty.Name} from {targetSection.Name}",
                            shouldLog: DebugConfigMerge);

                        targetSection.RemoveAllNamedEntries(incomingProperty.Name);
                        hasChanged = true;
                        break;
                    case CoalesceParseAction.Remove: // Type 4
                        {
                            if (targetSection.TryGetValue(incomingProperty.Name, out var values))
                            {
                                for (int i = values.Count - 1; i >= 0; i--)
                                {
                                    if (values[i].Value == prop.Value)
                                    {
                                        MLog.Debug($@"ConfigMerge::MergeEntry - Removing value {incomingProperty.Name}->{prop.Value} from {targetSection.Name}",
                                            shouldLog: DebugConfigMerge);
                                        values.RemoveAt(i); // Remove this value
                                        hasChanged = true;
                                    }
                                }
                            }
                        }
                        break;
                    default:
                        MLog.Warning($@"MERGE TYPE NOT IMPLEMENTED: {prop.ParseAction}");
                        break;
                }
            }

            return hasChanged;
        }

        /// <summary>
        /// Adjusts the typing of the values of this property if it is a double typed property name - e.g. it starts with !/., even after we identified the parsing type.
        /// </summary>
        /// <param name="incomingProperty">Property to account for.</param>
        /// <returns>The same property, but with the value type adjusted and name corrected if it was a double typed property.</returns>
        private static bool applyDoubleTypedItem(CoalesceSection targetSection, CoalesceProperty incomingProperty)
        {
            if (!ConfigFileProxy.IsTyped(incomingProperty.Name) || incomingProperty.Count == 0) return false;

            // Double typed
            var originalType = incomingProperty.First().ParseAction;
            var newType = ConfigFileProxy.GetIniDataType(incomingProperty.Name);
            incomingProperty.Name = ConfigFileProxy.StripType(incomingProperty.Name);

            // Change incoming typings to match the double type value
            for (int i = 0; i < incomingProperty.Count; i++)
            {
                var val = incomingProperty[i];
                val.ValueType = CoalesceValue.GetValueType(newType);
                incomingProperty[i] = val; // Struct assignment
            }

            if (originalType == CoalesceParseAction.Add)
            {
                targetSection.AddEntry(incomingProperty);
            }
            else if (originalType == CoalesceParseAction.AddUnique)
            {
                targetSection.AddEntryIfUnique(incomingProperty);
            }
            else
            {
                MLog.Warning($@"Double typed config delta has unsupported original typing: {originalType}. Must be Add or AddUnique only.");
            }

            return true;
        }

        public static void PerformDLCMerge(MEGame game, string dlcFolderRoot, string dlcFolderName)
        {
            var cookedDir = Path.Combine(dlcFolderRoot, dlcFolderName, game.CookedDirName());
            if (!Directory.Exists(cookedDir))
            {
                MLog.Error($@"Cannot DLC merge {dlcFolderName}, cooked directory doesn't exist: {cookedDir}");
                return; // Cannot asset merge
            }

            var configBundle = ConfigAssetBundle.FromDLCFolder(game, cookedDir, dlcFolderName);
            if (configBundle == null)
            {
                MLog.Error($@"Cannot DLC merge {dlcFolderName}, assets did not load");
                return; // Cannot asset merge
            }

            var m3cds = Directory.GetFiles(cookedDir, @"*" + ConfigMerge.CONFIG_MERGE_EXTENSION,
                    SearchOption.TopDirectoryOnly)
                .Where(x => Path.GetFileName(x).StartsWith(ConfigMerge.CONFIG_MERGE_PREFIX))
                .ToList(); // Find CoalescedMerge-*.m3cd files

            foreach (var m3cd in m3cds)
            {
                MLog.Information($@"Merging M3 Config Delta {m3cd} in {dlcFolderName}");
                var m3cdasset = ConfigFileProxy.LoadIni(m3cd);
                PerformMerge(configBundle, m3cdasset);
            }

            if (configBundle.HasChanges)
            {
                configBundle.CommitDLCAssets(); // Commit the final result
            }
        }

        /// <summary>
        /// Converts a configuration bundle object to a version that can be applied as an M3CD. This does not support double typing!
        /// </summary>
        /// <param name="configBundle">Bundle to convert</param>
        /// <returns></returns>
        public static string ConvertBundleToM3CD(ConfigAssetBundle configBundle)
        {
            var ini = new DuplicatingIni();
            foreach (var assetName in configBundle.GetAssetNames())
            {
                var asset = configBundle.GetAsset(assetName);
                foreach (var section in asset.Sections)
                {
                    var sectionHeader = $"{Path.GetFileNameWithoutExtension(assetName)}.ini {section.Key}";
                    var m3cdSection = ini.GetOrAddSection(sectionHeader);
                    foreach (var uniqueProperty in section.Value)
                    {
                        foreach (var multiProperty in uniqueProperty.Value)
                        {
                            m3cdSection.Entries.Add(new DuplicatingIni.IniEntry($"{ConfigFileProxy.GetGame2IniDataPrefix(multiProperty.ParseAction)}{uniqueProperty.Key}", multiProperty.Value));
                        }
                    }
                }
            }

            return ini.ToString();
        }
    }
}
