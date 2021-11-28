using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LegendaryExplorerCore.Gammtek.Extensions.Collections.Generic;
using LegendaryExplorerCore.Misc;
using ME3TweaksCore.Diagnostics;
using ME3TweaksCore.Helpers;
using ME3TweaksCore.Misc;
using ME3TweaksCore.Targets;
using Newtonsoft.Json;

namespace ME3TweaksCore.Services
{
    public partial class MOnlineContent
    {
        /// <summary>
        /// Basegame File Identification Service URLs
        /// </summary>
        public static FallbackLink BasegameFileIdentificationServiceURL { get; } = new FallbackLink()
        {
            MainURL = @"https://me3tweaks.com/modmanager/services/basegamefileidentificationservice",
            FallbackURL = @"https://raw.githubusercontent.com/ME3Tweaks/ME3TweaksModManager/master/MassEffectModManagerCore/staticfiles/basegamefileidentificationservice.json"
        };
    }
}

namespace ME3TweaksCore.Services.BasegameFileIdentification
{
    public class BasegameFileIdentificationService
    {
        private static Dictionary<string, CaseInsensitiveDictionary<List<BasegameFileRecord>>> LocalDatabase;
        private static Dictionary<string, CaseInsensitiveDictionary<List<BasegameFileRecord>>> ME3TweaksDatabase;

        /// <summary>
        /// If the BasegameFileIdentificationService has been initially loaded
        /// </summary>
        public static bool ServiceLoaded { get; private set; }

        private static void LoadLocalBasegameIdentificationService()
        {
            if (LocalDatabase != null) return;
            LocalDatabase = new CaseInsensitiveDictionary<CaseInsensitiveDictionary<List<BasegameFileRecord>>>();
            LoadDatabase(true, LocalDatabase);
        }

        internal static void LoadME3TweaksBasegameIdentificationService(bool forceRefresh)
        {
            if (LocalDatabase != null) return;
            ME3TweaksDatabase = FetchBasegameFileIdentificationServiceManifest(forceRefresh);
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
                    var db = getBlankBGFIDB();
                    database.ReplaceAll(db);
                }
            }
            else
            {
                MLog.Information($@"Loaded blank {typeStr} Basegame File Identification Service");
                var db = getBlankBGFIDB();
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
                var outText = JsonConvert.SerializeObject(LocalDatabase, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore });
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

        public static Dictionary<string, CaseInsensitiveDictionary<List<BasegameFileRecord>>> FetchBasegameFileIdentificationServiceManifest(bool overrideThrottling = false)
        {
            MLog.Information(@"Fetching basegame file identification manifest");

            //read cached first.
            string cached = null;
            if (File.Exists(MCoreFilesystem.GetME3TweaksBasegameFileIdentificationServiceFile()))
            {
                try
                {
                    cached = File.ReadAllText(MCoreFilesystem.GetME3TweaksBasegameFileIdentificationServiceFile());
                }
                catch (Exception e)
                {
                    TelemetryInterposer.TrackErrorWithLog(e, new Dictionary<string, string>()
                    {
                        {@"Error type", @"Error reading cached online content" },
                        {@"Service", @"Basegame File Identification Service" },
                        {@"Message", e.Message }
                    });
                }
            }


            if (!File.Exists(MCoreFilesystem.GetME3TweaksBasegameFileIdentificationServiceFile()) || overrideThrottling || MOnlineContent.CanFetchContentThrottleCheck())
            {
                foreach (var staticurl in MOnlineContent.BasegameFileIdentificationServiceURL.GetAllLinks())
                {
                    Uri myUri = new Uri(staticurl);
                    string host = myUri.Host;
                    try
                    {
                        using var wc = new ShortTimeoutWebClient();
                        string json = MOnlineContent.FetchRemoteString(staticurl);
                        File.WriteAllText(MCoreFilesystem.GetME3TweaksBasegameFileIdentificationServiceFile(), json);
                        return JsonConvert.DeserializeObject<Dictionary<string, CaseInsensitiveDictionary<List<BasegameFileRecord>>>>(json);
                    }
                    catch (Exception e)
                    {
                        //Unable to fetch latest help.
                        MLog.Error($@"Error fetching online basegame file identification service from endpoint {host}: {e.Message}");
                    }
                }

                if (cached == null)
                {
                    MLog.Error(@"Unable to load basegame file identification service and local file doesn't exist. Returning a blank copy.");
                    return getBlankBGFIDB();
                }
            }
            MLog.Information(@"Using cached BGFIS instead");

            try
            {
                return JsonConvert.DeserializeObject<Dictionary<string, CaseInsensitiveDictionary<List<BasegameFileRecord>>>>(cached);
            }
            catch (Exception e)
            {
                MLog.Error(@"Could not parse cached basegame file identification service file. Returning blank BFIS data instead. Reason: " + e.Message);
                return getBlankBGFIDB();
            }
        }

        /// <summary>
        /// Returns a blank Basegame Identification Database
        /// </summary>
        /// <returns></returns>
        private static Dictionary<string, CaseInsensitiveDictionary<List<BasegameFileRecord>>> getBlankBGFIDB()
        {
            return new Dictionary<string, CaseInsensitiveDictionary<List<BasegameFileRecord>>>
            {
                [@"ME1"] = new CaseInsensitiveDictionary<List<BasegameFileRecord>>(),
                [@"ME2"] = new CaseInsensitiveDictionary<List<BasegameFileRecord>>(),
                [@"ME3"] = new CaseInsensitiveDictionary<List<BasegameFileRecord>>(),
                [@"LE1"] = new CaseInsensitiveDictionary<List<BasegameFileRecord>>(),
                [@"LE2"] = new CaseInsensitiveDictionary<List<BasegameFileRecord>>(),
                [@"LE3"] = new CaseInsensitiveDictionary<List<BasegameFileRecord>>(),
                [@"LELauncher"] = new CaseInsensitiveDictionary<List<BasegameFileRecord>>(),
            };
        }

        public static bool LoadService(bool forceRefresh = false)
        {
            LoadLocalBasegameIdentificationService();
            LoadME3TweaksBasegameIdentificationService(forceRefresh);
            ServiceLoaded = true;
            return true;
        }

        public static IReadOnlyDictionary<string, CaseInsensitiveDictionary<List<BasegameFileRecord>>> GetAllServerEntries()
        {
            // Would prefer way to make it all read only but... ... ...
            return ME3TweaksDatabase;
        }
    }
}