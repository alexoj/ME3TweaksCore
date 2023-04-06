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

        private static bool MergeEntry(CoalesceSection value, CoalesceProperty property, MEGame game)
        {
            bool hasChanged = false;
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
                            hasChanged = true;
                        }
                        break;
                    case CoalesceParseAction.Add: // Type 2 - add
                        MLog.Debug($@"ConfigMerge::MergeEntry - Adding value {property.Name}->{prop.Value}", shouldLog: DebugConfigMerge);
                        value.AddEntry(new CoalesceProperty(property.Name, prop.Value)); // Add our property to the list
                        hasChanged = true;
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
                            hasChanged = true;

                        }
                        break;
                    case CoalesceParseAction.RemoveProperty: // Type 1
                        value.RemoveAllNamedEntries(property.Name);
                        hasChanged = true;
                        break;
                    case CoalesceParseAction.Remove: // Type 4
                        {
                            if (value.TryGetValue(property.Name, out var values))
                            {
                                for (int i = values.Count - 1; i >= 0; i--)
                                {
                                    if (values[i].Value == prop.Value)
                                    {
                                        MLog.Debug($@"ConfigMerge::MergeEntry - Removing value {property.Name}->{prop.Value} from {value.Name}",
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

        public static void PerformDLCMerge(MEGame game, string dlcFolderRoot, string dlcFolderName)
        {
            var cookedDir = Path.Combine(dlcFolderRoot, dlcFolderName, game.CookedDirName());
            var configBundle = ConfigAssetBundle.FromDLCFolder(game, cookedDir, dlcFolderName);

            var update = false;
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
        /// Handler for a bundle of config assets
        /// </summary>
        public class ConfigAssetBundle
        {
            /// <summary>
            /// The assets the make up DLC's config files
            /// </summary>
            private CaseInsensitiveDictionary<CoalesceAsset> Assets = new();

            /// <summary>
            /// Game this bundle is for
            /// </summary>
            public MEGame Game { get; private set; }

            /// <summary>
            /// If this bundle has pending changes that have not yet been committed
            /// </summary>
            public bool HasChanges { get; set; }

            private string DLCFolderName;
            private string CookedDir;

            /// <summary>
            /// Generates a ConfigAssetBundle from the specified single-file stream (.bin)
            /// </summary>
            /// <param name="game"></param>
            /// <param name="stream"></param>
            /// <param name="filename"></param>
            /// <exception cref="Exception"></exception>
            private ConfigAssetBundle(MEGame game, Stream stream, string filename = null)
            {
                Game = game;
                if (Game is MEGame.LE1 or MEGame.LE2)
                {
                    Assets = CoalescedConverter.DecompileLE1LE2ToAssets(stream, filename ?? @"Coalesced_INT.bin");
                }
                else if (Game == MEGame.LE3)
                {
                    Assets = CoalescedConverter.DecompileGame3ToAssets(stream, filename ?? @"Coalesced.bin", stripExtensions: true);

                }
                else
                {
                    throw new Exception($"{nameof(ConfigAssetBundle)} does not support game {game}");
                }
            }

            /// <summary>
            /// Generate a ConfigAssetBundle from the specified single packed file stream (.bin)
            /// </summary>
            /// <param name="game"></param>
            /// <param name="stream"></param>
            /// <param name="fileName"></param>
            /// <returns></returns>
            public static ConfigAssetBundle FromSingleStream(MEGame game, Stream stream, string fileName = null)
            {
                return new ConfigAssetBundle(game, stream, fileName);
            }

            /// <summary>
            /// Generate a ConfigAssetBundle from the specified single packed file (.bin)
            /// </summary>
            /// <param name="game"></param>
            /// <param name="singleFile"></param>
            /// <exception cref="Exception"></exception>
            public static ConfigAssetBundle FromSingleFile(MEGame game, string singleFile)
            {
                using var stream = File.OpenRead(singleFile);
                return new ConfigAssetBundle(game, stream, Path.GetFileName(singleFile));
            }

            /// <summary>
            /// Generates a bundle object based on the CookedPCConsole folder specified
            /// </summary>
            /// <param name="game">What game this bundle is for</param>
            /// <param name="cookedDir">The full path to the cooked directory</param>
            /// <param name="dlcFolderName">The name of the DLC folder, e.g. DLC_MOD_XXX</param>
            public static ConfigAssetBundle FromDLCFolder(MEGame game, string cookedDir, string dlcFolderName)
            {
                return new ConfigAssetBundle(game, cookedDir, dlcFolderName);
            }

            /// <summary>
            /// Generates a bundle object based on the CookedPCConsole folder specified
            /// </summary>
            /// <param name="game">What game this bundle is for</param>
            /// <param name="cookedDir">The full path to the cooked directory</param>
            /// <param name="dlcFolderName">The name of the DLC folder, e.g. DLC_MOD_XXX</param>
            private ConfigAssetBundle(MEGame game, string cookedDir, string dlcFolderName)
            {
                Game = game;
                CookedDir = cookedDir;
                DLCFolderName = dlcFolderName;

                if (game == MEGame.LE2)
                {
                    var iniFiles = Directory.GetFiles(cookedDir, "*.ini", SearchOption.TopDirectoryOnly);
                    foreach (var ini in iniFiles)
                    {
                        // Todo: Filter out
                        var fname = Path.GetFileNameWithoutExtension(ini);
                        Assets[fname] = ConfigFileProxy.LoadIni(ini);
                    }
                }
                else if (game == MEGame.LE3)
                {
                    var coalFile = Path.Combine(cookedDir, $"Default_{dlcFolderName}.bin");
                    Assets = CoalescedConverter.DecompileGame3ToAssets(coalFile, stripExtensions: true);
                }
                else
                {
                    throw new Exception($"{nameof(ConfigAssetBundle)} does not support game {game}");
                }
            }

            public CoalesceAsset GetAsset(string assetName, bool createIfNotFound = true)
            {
                var asset = Path.GetFileNameWithoutExtension(assetName);
                if (Assets.TryGetValue(asset, out var result))
                    return result;

                if (createIfNotFound)
                {
                    Assets[asset] = new CoalesceAsset($"{asset}.ini"); // Even game 3 uses .ini, I think...
                    return Assets[asset];
                }

                return null;
            }

            /// <summary>
            /// Commits this bundle to the specified single config file
            /// </summary>
            public void CommitAssets(string outPath)
            {
                if (Game is MEGame.LE1 or MEGame.LE2)
                {
                    // Combine the assets
                    var inis = new CaseInsensitiveDictionary<DuplicatingIni>();
                    foreach (var asset in Assets)
                    {
                        inis[asset.Key] = CoalesceAsset.ToIni(asset.Value);
                    }

                    var compiledStream = CoalescedConverter.CompileLE1LE2FromMemory(inis);
                    compiledStream.WriteToFile(outPath);
                    HasChanges = false;
                }
                else if (Game == MEGame.LE3)
                {
                    // This is kind of a hack, but it works.
                    var compiled = CoalescedConverter.CompileFromMemory(Assets.ToDictionary(x => x.Key, x => x.Value.ToXmlString()));
                    compiled.WriteToFile(outPath);
                    HasChanges = false;
                }
            }

            /// <summary>
            /// Commits this bundle to the same folder it was loaded from
            /// </summary>
            public void CommitDLCAssets()
            {
                if (Game == MEGame.LE2)
                {
                    foreach (var v in Assets)
                    {
                        var outFile = Path.Combine(CookedDir, v.Key + @".ini");
                        File.WriteAllText(outFile, v.Value.GetGame2IniText());
                    }
                    HasChanges = false;
                }
                else if (Game == MEGame.LE3)
                {
                    var coalFile = Path.Combine(CookedDir, $@"Default_{DLCFolderName}.bin");
                    CommitAssets(coalFile);
                    HasChanges = false;

                }
            }
        }
    }
}
