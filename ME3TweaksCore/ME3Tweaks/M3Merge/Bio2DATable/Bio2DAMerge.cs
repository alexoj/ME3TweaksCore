using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LegendaryExplorerCore.Coalesced;
using LegendaryExplorerCore.Coalesced.Config;
using LegendaryExplorerCore.GameFilesystem;
using LegendaryExplorerCore.Helpers;
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

        private static readonly string[] Mergable2DAFiles = new[]
        {
            "Engine.pcc",
            "SFXGame.pcc",
            "EntryMenu.pcc",

            // Bring Down The Sky
            "BIOG_2DA_UNC_AreaMap_X.pcc",
            "BIOG_2DA_UNC_GalaxyMap_X.pcc",
            "BIOG_2DA_UNC_GamerProfile_X.pcc",
            "BIOG_2DA_UNC_Movement_X.pcc",
            "BIOG_2DA_UNC_Music_X.pcc",
            "BIOG_2DA_UNC_Talents_X.pcc",
            "BIOG_2DA_UNC_TreasureTables_X.pcc",
            "BIOG_2DA_UNC_UI_X.pcc",
        };

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


            // Step 1: Load all modifiable packages
            var loadedFiles = target.GetFilesLoadedInGame();
            var packageContainer = new Bio2DAMergePackageContainer();
            foreach (var file in Mergable2DAFiles)
            {
                if (loadedFiles.TryGetValue(file, out var filepath))
                {
                    var package = MEPackageHandler.OpenMEPackage(filepath);
                    packageContainer.InsertTargetPackage(package);
                }
            }

            // Step 2: Reset all tables
            var vanillaTables = MEPackageHandler.OpenMEPackageFromStream(MUtilities.ExtractInternalFileToStream("ME3TweaksCore.ME3Tweaks.M3Merge.Bio2DATable.VanillaTables.pcc"));
            var vanilla2DAs = vanillaTables.Exports.Where(x => x.IsA("Bio2DA")).ToList();
            foreach (var file in packageContainer.GetTargetablePackages())
            {
                foreach (var exp in vanilla2DAs)
                {
                    var matchingExp = file.FindExport(exp.InstancedFullPath);
                    if (matchingExp != null)
                    {
                        // Reset the 2DA to prepare for changes
                        EntryImporter.ImportAndRelinkEntries(EntryImporter.PortingOption.ReplaceSingularWithRelink, exp,
                            file, matchingExp, true, new RelinkerOptionsPackage(), out _);
                    }
                }
            }


            foreach (var dlc in dlcMountsInOrder)
            {
                var dlcCookedPath = Path.Combine(target.GetDLCPath(), dlc, target.Game.CookedDirName());

                MLog.Information($@"Looking for {BIO2DA_MERGE_FILE_SUFFIX} files in {dlcCookedPath}");
                var m3das = Directory.GetFiles(dlcCookedPath, @"*" + BIO2DA_MERGE_FILE_SUFFIX, SearchOption.AllDirectories).ToList(); // Find all M3DA files
                MLog.Information($@"Found {m3das.Count} m3da files to parse");

                foreach (var m3daF in m3das)
                {
                    MLog.Information($@"Merging M3 Bio2DA Merge Manifest {m3daF}");
                    var result = MergeManifest(dlcCookedPath, m3daF, target, recordMerge, packageContainer);
                }


            }

            foreach (var file in packageContainer.GetTargetablePackages())
            {
                // Todo: Record merges for BGFIS

                if (file.IsModified)
                {
                    MLog.Information($"Saving 2DA merged package {file.FilePath}");
                    file.Save();
                }
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
        /// <param name="packageContainer"></param>
        /// <returns></returns>
        private static bool MergeManifest(string dlcCookedPath, string mergeFilePath, GameTarget target, Action<string> recordMerge, Bio2DAMergePackageContainer packageContainer)
        {
            var mergeData = File.ReadAllText(mergeFilePath);
            var mergeObject = JsonConvert.DeserializeObject<List<Bio2DAMergeManifest>>(mergeData);
            var mergedResult = false;

            foreach (var obj in mergeObject)
            {
                var destPackage = packageContainer.GetTargetPackage(Path.Combine(target.GetCookedPath(), obj.GamePackageFile));
                if (destPackage == null)
                {
                    MLog.Error($"Bio2DA merge 'packagefile' is invalid: {obj.GamePackageFile} - cannot merge into non-basegame/Bring Down The Sky 2DA files");
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
                    MLog.Error($"Bio2DA merge 'mergepackagefile' is invalid: {obj.GamePackageFile} - could not find in CookedPCConsole folder of mod");
                    return false;
                }

                var baseFile = packageContainer.GetTargetPackage(basePackagePath);
                var modFile = packageContainer.GetModPackage(modPackagePath);
                if (modFile == null)
                {
                    // Needs opened and cached
                    modFile = MEPackageHandler.OpenMEPackage(modPackagePath);
                    packageContainer.InsertModPackage(modFile);
                }
                foreach (var table in obj.ModTables)
                {
                    string objNameStr = table;
                    int dotIdx = objNameStr.LastIndexOf('.');
                    if (dotIdx > 0)
                    {
                        objNameStr = objNameStr[(dotIdx + 1)..];
                    }

                    var objName = NameReference.FromInstancedString(objNameStr);
                    if (!objName.Name.EndsWith("_part"))
                    {
                        MLog.Error($"Bio2DA merge 'mergetables' value is invalid: {table} - base name of object does not end with _part");
                        return false;
                    }

                    var tableName = objName.Name.Substring(0, objName.Name.Length - 5); // Remove _part. The table name should not be indexed... probably

                    var modTable = modFile.FindExport(table); // Find by IFP.
                    if (modTable == null)
                    {
                        MLog.Error($"Bio2DA merge 'mergetables' value is invalid: {table} - could not find table with that instanced full path in package '{modPackagePath}'");
                        return false;
                    }

                    if (!modTable.IsA("Bio2DA"))
                    {
                        MLog.Error($"Bio2DA merge 'mergetables' value is invalid: {table} - export is not a Bio2DA or subclass. It was: {modTable.ClassName}");
                        return false;
                    }

                    var baseTable = baseFile.Exports.FirstOrDefault(x => !x.IsDefaultObject && x.IsA("Bio2DA") && x.ObjectName.Instanced.CaseInsensitiveEquals(tableName));
                    if (baseTable == null)
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
                        mergedResult |= mergedCount.Any();
                        base2DA.Write2DAToExport();
                    }
                    else
                    {
                        MLog.Error($"Bio2DA merge into {tableName} from {table} failed with result {result}");
                        return false;
                    }
                }
            }

            return mergedResult;
        }
    }
}