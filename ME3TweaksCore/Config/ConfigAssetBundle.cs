using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LegendaryExplorerCore.Coalesced;
using LegendaryExplorerCore.Helpers;
using LegendaryExplorerCore.Misc;
using LegendaryExplorerCore.Packages;

namespace ME3TweaksCore.Config
{

    /// <summary>
    /// Handler for a bundle of config assets
    /// </summary>
    [DebuggerDisplay("ConfigAssetBundle for {DebugFileName}")]
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
#if DEBUG
            DebugFileName = filename;
#endif
            Game = game;
            if (Game is MEGame.LE1 or MEGame.LE2)
            {
                Assets = CoalescedConverter.DecompileLE1LE2ToAssets(stream, filename ?? @"Coalesced_INT.bin", stripExtensions: true);
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
#if DEBUG
        public string DebugFileName { get; set; }
#endif

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

#if DEBUG
            DebugFileName = dlcFolderName;
#endif

            if (game == MEGame.LE2)
            {
                var iniFiles = Directory.GetFiles(cookedDir, "*.ini", SearchOption.TopDirectoryOnly);
                foreach (var ini in iniFiles)
                {
                    var fname = Path.GetFileNameWithoutExtension(ini);
                    if (!CoalescedConverter.ProperNames.Contains(fname, StringComparer.InvariantCultureIgnoreCase))
                        continue; // Not supported.
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
        public void CommitDLCAssets(string outPath = null)
        {
            if (Game == MEGame.LE2)
            {
                foreach (var v in Assets)
                {
                    var outFile = Path.Combine(outPath ?? CookedDir, Path.GetFileNameWithoutExtension(v.Key) + @".ini");
                    File.WriteAllText(outFile, v.Value.GetGame2IniText());
                }
                HasChanges = false;
            }
            else if (Game == MEGame.LE3)
            {
                var coalFile = Path.Combine(outPath ?? CookedDir, $@"Default_{DLCFolderName}.bin");
                CommitAssets(coalFile);
                HasChanges = false;

            }
        }

        /// <summary>
        /// Merges this bundle into the specified one, applying the changes.
        /// </summary>
        /// <param name="destBundle"></param>
        public void MergeInto(ConfigAssetBundle destBundle)
        {
            foreach (var myAsset in Assets)
            {
                var matchingDestAsset = destBundle.GetAsset(myAsset.Key);
                foreach (var mySection in myAsset.Value.Sections)
                {
                    var matchingDestSection = matchingDestAsset.GetOrAddSection(mySection.Key);
                    foreach (var entry in mySection.Value)
                    {
                        ConfigMerge.MergeEntry(matchingDestSection, entry.Value, destBundle.Game);
                    }
                }
            }
        }
    }
}
