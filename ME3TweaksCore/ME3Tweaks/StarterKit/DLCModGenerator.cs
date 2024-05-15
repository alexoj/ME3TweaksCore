using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LegendaryExplorerCore.Coalesced;
using LegendaryExplorerCore.GameFilesystem;
using LegendaryExplorerCore.Misc;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.TLK;
using LegendaryExplorerCore.TLK.ME1;
using ME3TweaksCore.Diagnostics;
using ME3TweaksCore.Helpers;
using ME3TweaksCore.Localization;
using ME3TweaksCore.ME3Tweaks.M3Merge;
using ME3TweaksCore.Objects;

namespace ME3TweaksCore.ME3Tweaks.StarterKit
{
    /// <summary>
    /// Generates a blank DLC mod folder (does not include moddesc.ini)
    /// </summary>
    public class DLCModGenerator
    {
        public static string CreateStarterKitMod(string destFolder, StarterKitOptions skOption, Action<string> UITextCallback, out List<Action<DuplicatingIni>> moddescAddinDelegates)
        {
            //NamedBackgroundWorker nbw = new NamedBackgroundWorker(@"StarterKitThread");
            //nbw.DoWork += (sender, args) =>
            //{
            var dlcFolderName = $@"DLC_MOD_{skOption.ModDLCFolderNameSuffix}";
            var modPath = skOption.OutputFolderOverride ?? Path.Combine(destFolder, MUtilities.SanitizePath(skOption.ModName));
            if (skOption.OutputFolderOverride == null && Directory.Exists(modPath))
            {
                MUtilities.DeleteFilesAndFoldersRecursively(modPath);
            }

            Directory.CreateDirectory(modPath);

            //Creating DLC directories
            MLog.Information(@"Creating starter kit folders");
            var contentDirectory = Path.Combine(modPath, dlcFolderName);

            if (skOption.OutputFolderOverride != null && Directory.Exists(contentDirectory))
            {
                // Wipe out DLC folder target
                MUtilities.DeleteFilesAndFoldersRecursively(contentDirectory);
            }

            Directory.CreateDirectory(contentDirectory);

            var cookedDir = Directory.CreateDirectory(Path.Combine(contentDirectory, skOption.ModGame.CookedDirName())).FullName;
            if (skOption.ModGame.IsGame1())
            {
                //AutoLoad.ini
                var autoload = new DuplicatingIni();
                autoload[@"Packages"][@"GlobalTalkTable1"].Value = $@"{dlcFolderName}_GlobalTlk.GlobalTlk_tlk";

                autoload[@"GUI"][@"NameStrRef"].Value = skOption.ModInternalTLKID.ToString();

                autoload[@"ME1DLCMOUNT"][@"ModName"].Value = skOption.ModName;
                autoload[@"ME1DLCMOUNT"][@"ModMount"].Value = skOption.ModMountPriority.ToString();
                MLog.Information($@"Saving autoload.ini for {skOption.ModGame} mod");
                autoload.WriteToFile(Path.Combine(contentDirectory, @"AutoLoad.ini"), new UTF8Encoding(false));

                //TLK
                var dialogdir = skOption.ModGame == MEGame.ME1
                    ? Directory.CreateDirectory(Path.Combine(cookedDir, @"Packages", @"Dialog")).FullName
                    : cookedDir;
                var tlkGlobalFile = Path.Combine(dialogdir, $@"{dlcFolderName}_GlobalTlk");
                var extension = skOption.ModGame == MEGame.ME1 ? @"upk" : @"pcc";
                foreach (var lang in GameLanguage.GetLanguagesForGame(skOption.ModGame))
                {
                    var langExt = lang.FileCode == @"INT" ? "" : $@"_{lang.FileCode}"; // do not localize
                    var tlkPath = $@"{tlkGlobalFile}{langExt}.{extension}";
                    MUtilities.ExtractInternalFile($@"ME3TweaksCore.ME3Tweaks.StarterKit.{skOption.ModGame}.BlankTlkFile.{extension}", tlkPath, true);

                    var tlkFile = MEPackageHandler.OpenMEPackage(tlkPath);
                    var tlk1 = new ME1TalkFile(tlkFile.GetUExport(1));
                    var tlk2 = new ME1TalkFile(tlkFile.GetUExport(2));

                    tlk1.StringRefs[0].StringID = skOption.ModInternalTLKID;
                    tlk2.StringRefs[0].StringID = skOption.ModInternalTLKID;

                    tlk1.StringRefs[0].Data = skOption.ModInternalName;
                    tlk2.StringRefs[0].Data = skOption.ModInternalName;

                    var huff = new HuffmanCompression();
                    huff.LoadInputData(tlk1.StringRefs.ToList());
                    huff.SerializeTalkfileToExport(tlkFile.GetUExport(1));

                    huff = new HuffmanCompression();
                    huff.LoadInputData(tlk2.StringRefs.ToList());
                    huff.SerializeTalkfileToExport(tlkFile.GetUExport(2));
                    MLog.Information($@"Saving {tlkPath} TLK package");
                    tlkFile.Save();
                }
            }
            else
            {
                //ME2, ME3
                MountFile mf = new MountFile();
                mf.Game = skOption.ModGame;
                mf.MountFlags = skOption.ModMountFlag;
                mf.ME2Only_DLCFolderName = dlcFolderName;
                mf.ME2Only_DLCHumanName = skOption.ModName;
                mf.MountPriority = (ushort)skOption.ModMountPriority;
                mf.TLKID = skOption.ModInternalTLKID;
                MLog.Information(@"Saving mount.dlc file for mod");
                mf.WriteMountFile(Path.Combine(cookedDir, @"Mount.dlc"));

                if (skOption.ModGame.IsGame3())
                {
                    if (skOption.ModGame == MEGame.ME3)
                    {
                        //Extract Default.Sfar
                        MUtilities.ExtractInternalFile(@"ME3TweaksCore.ME3Tweaks.StarterKit.ME3.Default.sfar", Path.Combine(cookedDir, @"Default.sfar"), true);
                    }

                    //Generate Coalesced.bin for mod
                    var memory = MUtilities.ExtractInternalFileToStream(@"ME3TweaksCore.ME3Tweaks.StarterKit.Game3.Default_DLC_MOD_StarterKit.bin");
                    var files = CoalescedConverter.DecompileGame3ToMemory(memory);
                    //Modify coal files for this mod.
                    files[@"BioEngine.xml"] = files[@"BioEngine.xml"].Replace(@"StarterKit", skOption.ModDLCFolderNameSuffix); //update bioengine

                    var newMemory = CoalescedConverter.CompileFromMemory(files);
                    var outpath = Path.Combine(cookedDir, $@"Default_{dlcFolderName}.bin");
                    MLog.Information(@"Saving new starterkit coalesced file");
                    File.WriteAllBytes(outpath, newMemory.ToArray());
                }
                else
                {
                    //ME2, LE2
                    DuplicatingIni bioEngineIni = new DuplicatingIni();
                    //bioEngineIni.Configuration.AssigmentSpacer = ""; //no spacer.
                    bioEngineIni[@"Core.System"][@"!CookPaths"].Value = @"CLEAR";
                    bioEngineIni[@"Core.System"][@"+SeekFreePCPaths"].Value = $@"..\BIOGame\DLC\{dlcFolderName}\CookedPC"; // Is still CookedPC on LE
                    bioEngineIni[@"Engine.DLCModules"][dlcFolderName].Value = skOption.ModModuleNumber.ToString();
                    bioEngineIni[@"DLCInfo"][@"Version"].Value = 0.ToString(); //unknown
                    bioEngineIni[@"DLCInfo"][@"Flags"].Value = ((int)skOption.ModMountFlag.FlagValue).ToString();
                    bioEngineIni[@"DLCInfo"][@"Name"].Value = skOption.ModInternalTLKID.ToString();
                    MLog.Information(@"Saving BioEngine file");
                    bioEngineIni.WriteToFile(Path.Combine(cookedDir, @"BIOEngine.ini"), new UTF8Encoding(false));
                }

                var tlkFilePrefix = skOption.ModGame.IsGame3() ? dlcFolderName : $@"DLC_{skOption.ModModuleNumber}";

                var languages = GameLanguage.GetLanguagesForGame(skOption.ModGame);
                foreach (var lang in languages)
                {
                    List<TLKStringRef> strs = new List<TLKStringRef>();
                    strs.Add(new TLKStringRef(skOption.ModInternalTLKID, skOption.ModInternalName));
                    if (skOption.ModGame.IsGame2())
                    {
                        strs.Add(new TLKStringRef(skOption.ModInternalTLKID + 1, @"DLC_" + skOption.ModModuleNumber));
                    }
                    else
                    {
                        strs.Add(new TLKStringRef(skOption.ModInternalTLKID + 1, @"DLC_MOD_" + skOption.ModDLCFolderNameSuffix));
                    }

                    strs.Add(new TLKStringRef(skOption.ModInternalTLKID + 2, lang.LanguageCode.ToString()));
                    strs.Add(new TLKStringRef(skOption.ModInternalTLKID + 3, @"Male"));
                    strs.Add(new TLKStringRef(skOption.ModInternalTLKID + 3, @"Female"));

                    foreach (var str in strs)
                    {
                        str.Data += '\0';
                    }

                    var tlk = Path.Combine(cookedDir, $@"{tlkFilePrefix}_{lang.FileCode}.tlk");
                    MLog.Information(@"Saving TLK file: " + tlk);
                    LegendaryExplorerCore.TLK.ME2ME3.HuffmanCompression.SaveToTlkFile(tlk, strs);
                }
            }

            // ADDINS
            moddescAddinDelegates = new List<Action<DuplicatingIni>>();
            if (skOption.AddStartupFile)
            {
                UITextCallback?.Invoke($@"{LC.GetString(LC.string_generatingMod)} - Startup file");
                StarterKitAddins.AddStartupFile(skOption.ModGame, contentDirectory);
            }
            if (skOption.AddPlotManagerData)
            {
                UITextCallback?.Invoke($@"{LC.GetString(LC.string_generatingMod)} - PlotManager data");
                StarterKitAddins.GeneratePlotData(skOption.ModGame, contentDirectory);
            }

            if (skOption.AddModSettingsMenu)
            {
                UITextCallback?.Invoke($@"{LC.GetString(LC.string_generatingMod)} - Mod Settings Menu");
                StarterKitAddins.AddModSettingsMenu(null, skOption.ModGame, contentDirectory, moddescAddinDelegates);
            }

            if (skOption.Blank2DAsToGenerate.Any())
            {
                UITextCallback?.Invoke($@"{LC.GetString(LC.string_generatingMod)} - 2DAs");
                StarterKitAddins.GenerateBlank2DAs(skOption.ModGame, contentDirectory, skOption.Blank2DAsToGenerate);
            }

            // Generator needs to accept multiple outfit dictionaries
            var outfits = new List<Dictionary<string, object>>();
            string errorMessage = null;
            if (skOption.AddAshleySQM)
            {
                UITextCallback?.Invoke($@"{LC.GetString(LC.string_generatingMod)} - Ashley SQM");
                errorMessage = StarterKitAddins.GenerateSquadmateMergeFiles(skOption.ModGame, @"Ashley", contentDirectory, outfits);
            }
            if (errorMessage == null && skOption.AddEDISQM)
            {
                UITextCallback?.Invoke($@"{LC.GetString(LC.string_generatingMod)} - EDI SQM");
                errorMessage = StarterKitAddins.GenerateSquadmateMergeFiles(skOption.ModGame, @"EDI", contentDirectory, outfits);
            }
            if (errorMessage == null && skOption.AddGarrusSQM)
            {
                UITextCallback?.Invoke($@"{LC.GetString(LC.string_generatingMod)} - Garrus SQM");
                errorMessage = StarterKitAddins.GenerateSquadmateMergeFiles(skOption.ModGame, @"Garrus", contentDirectory, outfits);
            }
            if (errorMessage == null && skOption.AddKaidanSQM)
            {
                UITextCallback?.Invoke($@"{LC.GetString(LC.string_generatingMod)} - Kaidan SQM");
                errorMessage = StarterKitAddins.GenerateSquadmateMergeFiles(skOption.ModGame, @"Kaidan", contentDirectory, outfits);
            }
            if (errorMessage == null && skOption.AddJamesSQM)
            {
                UITextCallback?.Invoke($@"{LC.GetString(LC.string_generatingMod)} - James SQM");
                errorMessage = StarterKitAddins.GenerateSquadmateMergeFiles(skOption.ModGame, @"Marine", contentDirectory, outfits);
            }
            if (errorMessage == null && skOption.AddJavikSQM)
            {
                UITextCallback?.Invoke($@"{LC.GetString(LC.string_generatingMod)} - Javik SQM");
                errorMessage = StarterKitAddins.GenerateSquadmateMergeFiles(skOption.ModGame, @"Prothean", contentDirectory, outfits);
            }
            if (errorMessage == null && skOption.AddLiaraSQM)
            {
                UITextCallback?.Invoke($@"{LC.GetString(LC.string_generatingMod)} - Liara SQM");
                errorMessage = StarterKitAddins.GenerateSquadmateMergeFiles(skOption.ModGame, @"Liara", contentDirectory, outfits);
            }
            if (errorMessage == null && skOption.AddTaliSQM)
            {
                UITextCallback?.Invoke($@"{LC.GetString(LC.string_generatingMod)} - Tali SQM");
                errorMessage = StarterKitAddins.GenerateSquadmateMergeFiles(skOption.ModGame, @"Tali", contentDirectory, outfits);
            }

            if (errorMessage != null)
            {
                throw new Exception(errorMessage);
            }

            if (outfits.Any())
            {
                // Write the .sqm file
                var sqmText = StarterKitAddins.GenerateOutfitMergeText(skOption.ModGame, outfits);
                var sqmPath = Path.Combine(contentDirectory, skOption.ModGame.CookedDirName(), SQMOutfitMerge.SQUADMATE_MERGE_MANIFEST_FILE);
                File.WriteAllText(sqmPath, sqmText);
            }

            return modPath;
        }
    }
}
