using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LegendaryExplorerCore.Coalesced;
using LegendaryExplorerCore.Coalesced.Config;
using LegendaryExplorerCore.GameFilesystem;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Packages.CloningImportingAndRelinking;
using LegendaryExplorerCore.Unreal;
using LegendaryExplorerCore.Unreal.Classes;
using LegendaryExplorerCore.Unreal.ObjectInfo;
using ME3TweaksCore.Diagnostics;
using ME3TweaksCore.GameFilesystem;
using ME3TweaksCore.Helpers;
using ME3TweaksCore.ME3Tweaks.M3Merge.LE1Config;
using ME3TweaksCore.Targets;
using Newtonsoft.Json;

namespace ME3TweaksCore.ME3Tweaks.M3Merge.Bio2DATable
{
    /// <summary>
    /// Handles the Bio2DA merge feature.
    /// </summary>
    public class Bio2DAMerge
    {
        private const string BIO2DA_MERGE_FILE_SUFFIX = @".m3da";

        public static bool RunBio2DAMerge(GameTarget target)
        {
            MLog.Information($@"Performing Bio2DA Merge for game: {target.TargetPath}");

            //var coalescedStream = MUtilities.ExtractInternalFileToStream(@"ME3TweaksCore.ME3Tweaks.M3Merge.LE1Config.Coalesced_INT.bin");
            //var configBundle = ConfigAssetBundle.FromSingleStream(MEGame.LE1, coalescedStream);

            var dlcMountsInOrder = MELoadedDLC.GetDLCNamesInMountOrder(target.Game, target.TargetPath);

            // For BGFIS
            bool mergedAny = false;
            string recordedMergeName = @"";

            void recordMerge(string displayName)
            {
                mergedAny = true;
                recordedMergeName += displayName + "\n"; // do not localize
            }


            foreach (var dlc in dlcMountsInOrder)
            {
                var dlcCookedPath = Path.Combine(M3Directories.GetDLCPath(target), dlc, target.Game.CookedDirName());

                MLog.Information($@"Looking for {BIO2DA_MERGE_FILE_SUFFIX} files in {dlcCookedPath}");
                var m3das = Directory.GetFiles(dlcCookedPath, BIO2DA_MERGE_FILE_SUFFIX, SearchOption.AllDirectories).ToList(); // Find all M3DA files
                MLog.Information($@"Found {m3das.Count} m3da files to parse");

                PackageCache cache = new PackageCache();
                foreach (var m3daF in m3das)
                {
                    MLog.Information($@"Merging M3 Bio2DA Merge Manifest {m3daF}");
                    var result = MergeManifest(dlcCookedPath, m3daF, target, recordMerge, cache);
                }

                // Todo: RecordMerge for BGFIS, Save basegame packages in cache to disk

            }


            return mergedAny;
        }

        /// <summary>
        /// Merges a single manifest file (can contain multiple files to merge)
        /// </summary>
        /// <param name="dlcCookedPath"></param>
        /// <param name="mergeFilePath"></param>
        /// <param name="target"></param>
        /// <param name="recordMerge"></param>
        /// <param name="cache"></param>
        /// <returns></returns>
        private static bool MergeManifest(string dlcCookedPath, string mergeFilePath, GameTarget target, Action<string> recordMerge, PackageCache cache)
        {
            var mergeData = File.ReadAllText(mergeFilePath);
            var mergeObject = JsonConvert.DeserializeObject<List<Bio2DAMergeManifest>>(mergeData);

            foreach (var obj in mergeObject)
            {
                if (!EntryImporter.FilesSafeToImportFrom(target.Game).Contains(obj.GamePackageFile, StringComparer.InvariantCultureIgnoreCase))
                {
                    MLog.Error($"Bio2DA merge 'packagefile' is invalid: {obj.GamePackageFile} - cannot merge into non-basegame only file");
                    return false;
                }

                var basePackagePath = Path.Combine(target.GetCookedPath(), obj.GamePackageFile);
                if (!File.Exists(basePackagePath))
                {
                    MLog.Error($"Bio2DA merge 'packagefile' is invalid: {obj.GamePackageFile} - could not find in basegame CookedPCConsole folder of target");
                    return false;
                }

                var modPackagePath = Directory.GetFileSystemEntries(dlcCookedPath, obj.ModPackageFile, SearchOption.AllDirectories).FirstOrDefault();
                if (modPackagePath == null)
                {
                    MLog.Error($"Bio2DA merge 'modpackagefile' is invalid: {obj.GamePackageFile} - could not find in CookedPCConsole folder of mod");
                    return false;
                }

                var baseFile = cache.GetCachedPackage(basePackagePath);
                var modFile = cache.GetCachedPackage(modPackagePath);

                foreach (var table in obj.ModTables)
                {
                    var objName = NameReference.FromInstancedString(table);
                    if (!objName.Name.EndsWith("_part"))
                    {
                        MLog.Error($"Bio2DA merge 'mergetables' value is invalid: {table} - base name of object does not end with _part");
                        return false;
                    }

                    var tableName = objName.Name.Substring(0, objName.Name.Length - 5); // Remove _part. The table name should not be indexed... probably

                    var modTable = modFile.Exports.FirstOrDefault(x => x.IsA("Bio2DA") && x.ObjectName.Instanced == objName);
                    if (modTable == null)
                    {
                        MLog.Error($"Bio2DA merge 'mergetables' value is invalid: {table} - could not find table with that name in package '{modPackagePath}'");
                        return false;
                    }

                    var baseTable = baseFile.Exports.FirstOrDefault(x => !x.IsDefaultObject && x.IsA("Bio2DA") && x.ObjectName.Instanced == tableName);
                    if(baseTable == null)
                    {
                        MLog.Error($"Bio2DA merge 'mergetables' value is invalid: {table} - could not find basegame table with base name '{tableName}' name in package '{basePackagePath}'");
                        return false;
                    }

                    Bio2DA mod2DA = new Bio2DA(modTable);
                    Bio2DA base2DA = new Bio2DA(baseTable);
                    var mergedCount = mod2DA.MergeInto(base2DA, out var result);
                    if (result == Bio2DAMergeResult.OK)
                    {
                        MLog.Information($"Bio2DA merged {mergedCount.Count} rows from {table} into {tableName}");
                    }
                    else
                    {
                        MLog.Error($"Bio2DA merge into {tableName} from {table} failed with result {result}");
                        return false;
                    }
                }
            }

            return false;
        }
    }
}