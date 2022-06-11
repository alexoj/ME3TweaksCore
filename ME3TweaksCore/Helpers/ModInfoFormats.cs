using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LegendaryExplorerCore.Helpers;
using LegendaryExplorerCore.Packages;
using ME3TweaksCore.Diagnostics;
using ME3TweaksCore.GameFilesystem;

namespace ME3TweaksCore.Helpers
{

    public static class ModFileFormats
    {

        public static MEGame GetGameMEMFileIsFor(string file)
        {
            try
            {
                MEGame game = MEGame.Unknown;
                using var memFile = File.OpenRead(file);
                var magic = memFile.ReadStringASCII(4);
                if (magic != @"TMOD")
                {
                    return game;
                }
                var version = memFile.ReadInt32(); //3 = LE
                var gameIdOffset = memFile.ReadInt64();
                memFile.Position = gameIdOffset;
                var gameId = memFile.ReadInt32();

                if (gameId == 1) game = version < 3 ? MEGame.ME1 : MEGame.LE1;
                if (gameId == 2) game = version < 3 ? MEGame.ME2 : MEGame.LE2;
                if (gameId == 3) game = version < 3 ? MEGame.ME3 : MEGame.LE3;
                return game;
            }
            catch (Exception e)
            {
                MLog.Exception(e, $@"Unable to determine game MEM file {file} is for");
                return MEGame.Unknown;
            }
        }

        // Mod files are NOT supported in M3
#if ALOT
        public static ModFileInfo GetGameForMod(string file)
        {
            try
            {
                using var modFile = File.OpenRead(file);
                var len = modFile.ReadInt32(); //first 4 bytes
                var version = modFile.ReadStringASCIINull();
                modFile.SeekBegin();
                if (version.Length >= 5) // "modern" .mod
                {
                    //Re-read the version length
                    version = modFile.ReadUnrealString();
                }
                var numEntries = modFile.ReadUInt32();
                string desc = modFile.ReadUnrealString();
                var script = modFile.ReadUnrealString().Split("\n").Select(x => x.Trim()).ToList(); // do not localize
                ApplicableGame game = ApplicableGame.None;
                if (script.Any(x => x.StartsWith(@"using ME1Explorer")))
                {
                    game |= ApplicableGame.ME1;
                }
                else if (script.Any(x => x.StartsWith(@"using ME2Explorer")))
                {
                    game |= ApplicableGame.ME2;
                }
                else if (script.Any(x => x.StartsWith(@"using ME3Explorer")))
                {
                    game |= ApplicableGame.ME3;
                }

                var target = Locations.GetTarget(game.ApplicableGameToMEGame());
                if (target == null)
                {
                    return new ModFileInfo()
                    {
                        ApplicableGames = ApplicableGame.None,
                        Description = $"Target game ({game.ApplicableGameToMEGame()}) is not installed",
                        Usable = false
                    };
                }

                var biogame = M3Directories.GetBioGamePath(target);
                foreach (var pcc in script.Where(x => x.StartsWith(@"pccs.Add(")))  // do not localize
                {
                    var subBioPath = pcc.Substring("pccs.Add(\"".Length); // do not localize
                    subBioPath = subBioPath.Substring(0, subBioPath.Length - 3);
                    var targetFile = Path.Combine(biogame, subBioPath);
                    if (!File.Exists(targetFile))
                    {
                        return new ModFileInfo()
                        {
                            ApplicableGames = ApplicableGame.None,
                            Description = $"Target file doesn't exist: {subBioPath}",
                            Usable = false
                        };
                    }
                }

                return new ModFileInfo()
                {
                    ApplicableGames = game,
                    Description = desc,
                    Usable = true
                };
            }
            catch (Exception e)
            {
                return new ModFileInfo()
                {
                    ApplicableGames = ApplicableGame.None,
                    Description = e.Message,
                    Usable = false
                };
            }


            //string path = "";
            //if (desc.Contains("Binary Replacement"))
            //{
            //    try
            //    {
            //        ParseME3xBinaryScriptMod(scriptLegacy, ref package, ref mod.exportId, ref path);
            //        if (mod.exportId == -1 || package == "" || path == "")
            //        {
            //            // NOT COMPATIBLE
            //            return ApplicableGame.None;
            //        }
            //    }
            //    catch
            //    {
            //        // NOT COMPATIBLE
            //        return ApplicableGame.None;
            //    }
            //    mod.packagePath = Path.Combine(path, package);
            //    mod.binaryModType = 1;
            //    len = modFile.ReadInt32();
            //    mod.data = modFile.ReadToBuffer(len);
            //}
            //else
            //{
            //    modFile.SeekBegin();
            //    len = modFile.ReadInt32();
            //    version = modFile.ReadStringASCII(len); // version
            //}

        }
#endif

    }
}