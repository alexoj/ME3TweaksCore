using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LegendaryExplorerCore.Gammtek.Extensions.Collections.Generic;
using LegendaryExplorerCore.Misc;
using ME3TweaksCore.Diagnostics;
using ME3TweaksCore.Helpers;
using ME3TweaksCore.Targets;
using Newtonsoft.Json;

namespace ME3TweaksCore.Services.BasegameFileIdentification
{

    public class BasegameFileIdentificationService
    {
        /// <summary>
        /// Call when a blank database needs to be returned (e.g. load failed)
        /// </summary>
        /// <returns></returns>
        internal static Dictionary<string, CaseInsensitiveDictionary<List<BasegameFileRecord>>> GetBlankDatabase() => new();

        private static Dictionary<string, CaseInsensitiveDictionary<List<BasegameFileRecord>>> LocalDatabase;
        private static Dictionary<string, CaseInsensitiveDictionary<List<BasegameFileRecord>>> ME3TweaksDatabase;

        private static void LoadLocalBasegameIdentificationService()
        {
            if (LocalDatabase != null) return;
            LocalDatabase = new CaseInsensitiveDictionary<CaseInsensitiveDictionary<List<BasegameFileRecord>>>();
            LoadDatabase(true, LocalDatabase);
        }

        internal static void LoadME3TweaksBasegameIdentificationService()
        {
            if (LocalDatabase != null) return;
            ME3TweaksDatabase = new CaseInsensitiveDictionary<CaseInsensitiveDictionary<List<BasegameFileRecord>>>();
            LoadDatabase(false, ME3TweaksDatabase);
        }

        private static void LoadDatabase(bool local, Dictionary<string, CaseInsensitiveDictionary<List<BasegameFileRecord>>> database)
        {
            var typeStr = local ? "Local" : "ME3Tweaks";
            var file = local ? MCoreFilesystem.GetLocalBasegameIdentificationServiceFile() : MCoreFilesystem.GetME3TweaksBasegameFileIdentificationServiceFile();
            if (File.Exists(file))
            {
                try
                {
                    var db = JsonConvert
                            .DeserializeObject<
                                Dictionary<string, CaseInsensitiveDictionary<
                                    List<BasegameFileRecord>>>>(
                                File.ReadAllText(file));
                    database.ReplaceAll(db);
                    MLog.Information($@"Loaded {typeStr} Basegame File Identification Service");
                }
                catch (Exception e)
                {
                    MLog.Error($@"Error loading {typeStr} Basegame File Identification Service: {e.Message}");
                    var db = GetBlankDatabase();
                    database.ReplaceAll(db);
                }
            }
            else
            {
                MLog.Information($@"Loaded blank {typeStr} Basegame File Identification Service");
                var db = GetBlankDatabase();
                database.ReplaceAll(db);
            }
        }

        public static void AddLocalBasegameIdentificationEntries(List<BasegameFileRecord> entries)
        {
            LoadLocalBasegameIdentificationService();

            bool updated = false;
            // Update the DB
            foreach (var entry in entries)
            {
                string gameKey = entry.game == @"0" ? @"LELAUNCHER" : MUtilities.GetGameFromNumber(entry.game).ToString();
                if (LocalDatabase.TryGetValue(gameKey, out var gameDB))
                {
                    List<BasegameFileRecord> existingInfos;
                    if (!gameDB.TryGetValue(entry.file, out existingInfos))
                    {
                        existingInfos = new List<BasegameFileRecord>();
                        gameDB[entry.file] = existingInfos;
                    }

                    if (existingInfos.All(x => x.hash != entry.hash))
                    {
                        // new info
                        entry.file = null; // Do not serialize this
                        entry.game = null; // Do not serialize this
                        existingInfos.Add(entry);
                        updated = true;
                    }
                }
            }

            // Serialize it back to disk
            if (updated)
            {
#if DEBUG
                var outText = JsonConvert.SerializeObject(LocalDatabase, Formatting.Indented, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore });
#else
                var outText = JsonConvert.SerializeObject(Database, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore });
#endif
                try
                {
                    File.WriteAllText(MCoreFilesystem.GetLocalBasegameIdentificationServiceFile(), outText);
                    MLog.Information(@"Updated Local Basegame File Identification Service");

                }
                catch (Exception e)
                {
                    // bwomp bwomp
                    MLog.Error($@"Error saving local BGFIS: {e.Message}");
                }
            }
            else
            {
                MLog.Information(@"Local Basegame File Identification Service did not need updating");

            }
        }

        /// <summary>
        /// Looks up information about a basegame file using the Basegame File Identification Service
        /// </summary>
        /// <param name="target"></param>
        /// <param name="fullfilepath"></param>
        /// <returns></returns>
        public static BasegameFileRecord GetBasegameFileSource(GameTarget target, string fullfilepath, string md5 = null)
        {
            // Check local first
            LoadLocalBasegameIdentificationService();
            if (LocalDatabase.TryGetValue(target.Game.ToString(), out var infosForGameL))
            {
                var relativeFilename = fullfilepath.Substring(target.TargetPath.Length + 1).ToUpper();

                if (infosForGameL.TryGetValue(relativeFilename, out var items))
                {
                    md5 ??= MUtilities.CalculateMD5(fullfilepath);
                    var match = items.FirstOrDefault(x => x.hash == md5);
                    if (match != null)
                    {
                        return match;
                    }
                }
            }


            // Local not found. Try server version instead.
            if (TryGetServerBasegameRecordsForGame(target.Game.ToString(), out var infosForGame))
            {
                var relativeFilename = fullfilepath.Substring(target.TargetPath.Length + 1).ToUpper();

                if (infosForGame.TryGetValue(relativeFilename, out var items))
                {
                    md5 ??= MUtilities.CalculateMD5(fullfilepath);
                    return items.FirstOrDefault(x => x.hash == md5);
                }
            }

            return null;
        }

        /// <summary>
        /// Gets the list of basegame records from the ME3Tweaks database for the specified game
        /// </summary>
        /// <param name="gameAsString"></param>
        /// <param name="infosForGame"></param>
        /// <returns></returns>
        private static bool TryGetServerBasegameRecordsForGame(string gameAsString, out CaseInsensitiveDictionary<List<BasegameFileRecord>> infosForGame)
        {
            if (ME3TweaksDatabase != null && ME3TweaksDatabase.TryGetValue(gameAsString, out var recordsList))
            {
                infosForGame = recordsList;
                return true;
            }

            infosForGame = new CaseInsensitiveDictionary<List<BasegameFileRecord>>(0); // None
            return false;
        }
    }
}