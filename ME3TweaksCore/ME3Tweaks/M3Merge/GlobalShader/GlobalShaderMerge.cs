using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BCnEncoder.Shared;
using LegendaryExplorerCore.Coalesced;
using LegendaryExplorerCore.Coalesced.Config;
using LegendaryExplorerCore.GameFilesystem;
using LegendaryExplorerCore.Helpers;
using LegendaryExplorerCore.Misc;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Unreal.BinaryConverters;
using ME3TweaksCore.Diagnostics;
using ME3TweaksCore.GameFilesystem;
using ME3TweaksCore.Helpers;
using ME3TweaksCore.Localization;
using ME3TweaksCore.ME3Tweaks.M3Merge.LE1Config;
using ME3TweaksCore.Services.Backup;
using ME3TweaksCore.Services.Shared.BasegameFileIdentification;
using ME3TweaksCore.Targets;

namespace ME3TweaksCore.ME3Tweaks.M3Merge.GlobalShader
{
    /// <summary>
    /// Handles merging global shader cache for LE games.
    /// </summary>
    public class GlobalShaderMerge
    {
        private static string SHADER_MERGE_PATTERN = @"GlobalShader-*.m3gs";

        public static bool RunShaderMerge(GameTarget target, bool log)
        {
            MLog.Information($@"Performing Shader Merge for game: {target.TargetPath}");
            var backup = BackupService.GetGameBackupPath(target.Game);
            if (backup == null)
            {
                MLog.Warning("Backup not available; cannot perform GlobalShaderCache merge. Skipping.");
                return false;
            }

            var globalShaderCacheF = Path.Combine(backup, "BioGame", "CookedPCConsole", "GlobalShaderCache-PC-D3D-SM5.upk");
            using var fs = File.OpenRead(globalShaderCacheF);
            var globalShaderCache = ShaderCache.ReadGlobalShaderCache(fs);

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
                var dlcCookedPath = Path.Combine(target.GetDLCPath(), dlc, target.Game.CookedDirName());

                MLog.Information($@"Looking for GlobalShader-*.m3gs files in {dlcCookedPath}", log);
                var globalShaders = Directory.GetFiles(dlcCookedPath, GlobalShaderMerge.SHADER_MERGE_PATTERN, SearchOption.TopDirectoryOnly)
                    .ToList();
                MLog.Information($@"Found {globalShaders.Count} m3gs files to apply", log);

                foreach (var gs in globalShaders)
                {
                    MLog.Information($@"Merging M3 Global Shader {gs}");
                    using var shaderStream = File.OpenRead(gs);
                    var shaderGuid = shaderStream.ReadGuid();
                    if (globalShaderCache.Shaders.TryGetValue(shaderGuid, out var shader))
                    {
                        shader.ShaderByteCode = shaderStream.ReadToBuffer(shaderStream.Length - shaderStream.Position);
                        recordMerge($@"{dlc}-{Path.GetFileName(gs)}");
                        mergedAny = true;
                    }
                    else
                    {
                        MLog.Error($@"Shader guid not found in GlobalShaderCache: {shaderGuid}");
                    }
                }
            }

            var records = new List<BasegameFileRecord>();
            var outF = Path.Combine(target.GetCookedPath(), "GlobalShaderCache-PC-D3D-SM5.upk");

            // Set the BGFIS record name
            if (mergedAny)
            {
                // Serialize the assets
                var ms = new MemoryStream();
                var gscsc = new ShaderCache.GlobalShaderCacheSerializingContainer(ms, null) { ActualGame = target.Game };
                globalShaderCache.WriteTo(gscsc);
                ms.WriteByte(0); // This forces size change, which lets us tell it's been modified on size check.
                ms.WriteToFile(outF);

                // Submit to BGFIS
                records.Add(new BasegameFileRecord(outF, target, recordedMergeName.Trim()));
                BasegameFileIdentificationService.AddLocalBasegameIdentificationEntries(records);
            }
            else
            {
                File.Copy(globalShaderCacheF, outF, true);
            }

            return true;
        }

        public static bool NeedsMerged(GameTarget target)
        {
            return true;
            var supercedances = target.GetFileSupercedances([".m3gs"]); //, viaTOC: true);
        }
    }
}