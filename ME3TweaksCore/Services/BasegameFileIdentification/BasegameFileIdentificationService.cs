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
using Newtonsoft.Json.Linq;

namespace ME3TweaksCore.Services
{
    public partial class MOnlineContent
    {
        /// <summary>
        /// {ServiceLoggingName} URLs
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
        /// <summary>
        /// Database of locally installed files
        /// </summary>
        private static Dictionary<string, CaseInsensitiveDictionary<List<BasegameFileRecord>>> LocalDatabase;
        /// <summary>
        /// Database of known hashes from ME3Tweaks. This list is not well maintained due to the amount of work it requires.
        /// </summary>
        private static Dictionary<string, CaseInsensitiveDictionary<List<BasegameFileRecord>>> ME3TweaksDatabase;

        /// <summary>
        /// If the BasegameFileIdentificationService has been initially loaded
        /// </summary>
        public static bool ServiceLoaded { get; private set; }

        /// <summary>
        /// Service name for logging
        /// </summary>
        private const string ServiceLoggingName = @"Basegame File Identification Service";

        private static string GetME3TweaksServiceCacheFile() => MCoreFilesystem.GetME3TweaksBasegameFileIdentificationServiceFile();


        private static void LoadLocalBasegameIdentificationService()
        {
            if (LocalDatabase != null) return;
            LocalDatabase = new CaseInsensitiveDictionary<CaseInsensitiveDictionary<List<BasegameFileRecord>>>();
            LoadDatabase(true, LocalDatabase);
        }

        private static bool LoadME3TweaksBasegameIdentificationService(JToken data)
        {
            // ServiceLoader for online content
            return InternalLoadME3TweaksService(data);
        }

        private static void LoadDatabase(bool local, Dictionary<string, CaseInsensitiveDictionary<List<BasegameFileRecord>>> database, JToken serverData = null)
        {
            var typeStr = local ? "Local" : "ME3Tweaks";
            var file = local ? MCoreFilesystem.GetLocalBasegameIdentificationServiceFile() : GetME3TweaksServiceCacheFile();
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
                    MLog.Information($@"Loaded {typeStr} {ServiceLoggingName}");
                }
                catch (Exception e)
                {
                    MLog.Error($@"Error loading {typeStr} {ServiceLoggingName}: {e.Message}");
                    var db = getBlankBGFIDB();
                    database.ReplaceAll(db);
                }
            }
            else
            {
                MLog.Information($@"Loaded blank {typeStr} {ServiceLoggingName}");
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
                    MLog.Information(@"Updated Local {ServiceLoggingName}");

                }
                catch (Exception e)
                {
                    // bwomp bwomp
                    MLog.Error($@"Error saving local BGFIS: {e.Message}");
                }
            }
            else
            {
                MLog.Information(@"Local {ServiceLoggingName} did not need updating");

            }
        }

        /// <summary>
        /// Looks up information about a basegame file using the {ServiceLoggingName}
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

        private static bool InternalLoadME3TweaksService(JToken serviceData)
        {
            // Online first
            if (serviceData != null)
            {
                try
                {
                    ME3TweaksDatabase = serviceData.ToObject<Dictionary<string, CaseInsensitiveDictionary<List<BasegameFileRecord>>>>();
                    ServiceLoaded = true;
#if DEBUG
                    File.WriteAllText(GetME3TweaksServiceCacheFile(), serviceData.ToString(Formatting.Indented));
#else
                    File.WriteAllText(GetME3TweaksServiceCacheFile(), serviceData.ToString(Formatting.None));
#endif
                    MLog.Information($@"Loaded online {ServiceLoggingName}");
                    return true;
                }
                catch (Exception ex)
                {
                    if (ServiceLoaded)
                    {
                        MLog.Error($@"Loaded online {ServiceLoggingName}, but failed to cache to disk: {ex.Message}");
                        return true;
                    }
                    else
                    {
                        MLog.Error($@"Failed to load {ServiceLoggingName}: {ex.Message}");
                        return false;
                    }
                }
            }

            // Use cached if online is not available
            if (File.Exists(GetME3TweaksServiceCacheFile()))
            {
                try
                {
                    var cached = File.ReadAllText(GetME3TweaksServiceCacheFile());
                    ME3TweaksDatabase = JsonConvert.DeserializeObject<Dictionary<string, CaseInsensitiveDictionary<List<BasegameFileRecord>>>>(cached);
                    ServiceLoaded = true;
                    MLog.Information($@"Loaded cached {ServiceLoggingName}");
                    return true;
                }
                catch (Exception e)
                {
                    MLog.Error($@"Failed to load cached {ServiceLoggingName}: {e.Message}");
                    var relevantInfo = new Dictionary<string, string>()
                    {
                        {@"Error type", @"Error reading cached online content"},
                        {@"Service", ServiceLoggingName},
                        {@"Message", e.Message}
                    };
                    TelemetryInterposer.UploadErrorLog(e, relevantInfo);
                }
            }

            MLog.Information($@"Unable to load {ServiceLoggingName} service: No cached content or online content was available to load");
            return false;
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

        public static bool LoadService(JToken data)
        {
            LoadLocalBasegameIdentificationService();
            var result = LoadME3TweaksBasegameIdentificationService(data);
            ServiceLoaded = true;
            return result;
        }

        public static IReadOnlyDictionary<string, CaseInsensitiveDictionary<List<BasegameFileRecord>>> GetAllServerEntries()
        {
            // Would prefer way to make it all read only but... ... ...
            return ME3TweaksDatabase;
        }
    }
}