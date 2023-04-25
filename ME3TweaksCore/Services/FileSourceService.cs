using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LegendaryExplorerCore.Gammtek.Extensions.Collections.Generic;
using LegendaryExplorerCore.Misc;
using LegendaryExplorerCore.Packages;
using ME3TweaksCore.Diagnostics;
using ME3TweaksCore.Helpers;
using ME3TweaksCore.Misc;
using ME3TweaksCore.Services.BasegameFileIdentification;
using ME3TweaksCore.Targets;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ME3TweaksCore.Services
{
    public class FileSourceService
    {
        /// <summary>
        /// Database of hash to download link
        /// </summary>
        private static CaseInsensitiveDictionary<string> Database;

        /// <summary>
        /// If the FileSourceService has been initially loaded
        /// </summary>
        public static bool ServiceLoaded { get; private set; }

        /// <summary>
        /// Service name for logging
        /// </summary>
        private const string ServiceLoggingName = @"File Source Service";

        private static void LoadFileSourceService()
        {
            if (Database != null) return;
            Database = new CaseInsensitiveDictionary<string>();
            LoadDatabase(Database);
        }


        private static void LoadDatabase(CaseInsensitiveDictionary<string> database, JToken serverData = null)
        {
            var file = MCoreFilesystem.GetFileSourceServiceFile();
            if (File.Exists(file))
            {
                try
                {
                    var db = JsonConvert.DeserializeObject<CaseInsensitiveDictionary<string>>(File.ReadAllText(file));
                    database.ReplaceAll(db);
                    MLog.Information($@"Loaded {ServiceLoggingName}");
                }
                catch (Exception e)
                {
                    MLog.Error($@"Error loading {ServiceLoggingName}: {e.Message}");
                }
            }
            else
            {
                MLog.Information($@"Loaded blank {ServiceLoggingName}");
                database.Clear();
            }
        }

        public static void AddFileSourceEntries(CaseInsensitiveDictionary<string> entries)
        {
            LoadFileSourceService();

            bool updated = false;
            // Update the DB
            foreach (var entry in entries)
            {
                if (Database.TryAdd(entry.Key, entry.Value))
                {
                    updated = true;
                }
            }

            // Serialize it back to disk
            if (updated)
            {
                CommitDatabaseToDisk();
            }
            else
            {
                MLog.Information($@"{ServiceLoggingName} did not need updating");
            }
        }

        private static void CommitDatabaseToDisk()
        {

#if DEBUG
            var outText = JsonConvert.SerializeObject(Database, Formatting.Indented,
                new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore });
#else
                var outText =
 JsonConvert.SerializeObject(Database, new JsonSerializerSettings() { NullValueHandling =
 NullValueHandling.Ignore });
#endif
            try
            {
                File.WriteAllText(MCoreFilesystem.GetFileSourceServiceFile(), outText);
                MLog.Information($@"Updated local {ServiceLoggingName}");

            }
            catch (Exception e)
            {
                // bwomp bwomp
                MLog.Error($@"Error saving local BGFIS: {e.Message}");
            }
        }

        /// <summary>
        /// Looks up information about a basegame file using the {ServiceLoggingName}
        /// </summary>
        /// <param name="target"></param>
        /// <param name="fullfilepath"></param>
        /// <returns></returns>
        public static string GetFileSource(string md5)
        {
            LoadFileSourceService();
            if (Database.TryGetValue(md5, out var link))
            {
                return link;
            }
            return null;
        }

        // This service doesn't take any data but uses the service loader model.
        // It may in future
        public static bool LoadService(JToken data)
        {
            LoadFileSourceService();
            ServiceLoaded = true;
            return true;
        }

        private static object syncObj = new object();

        /// <summary>
        /// Purges all entries for the specified game and commits the file back to disk.
        /// </summary>
        /// <param name="game">The game to purge entries for</param>
        public static void PurgeEntry(string md5)
        {
            lock (syncObj)
            {
                if (Database.Remove(md5))
                {
                    MLog.Information($@"Removed {md5} from {ServiceLoggingName}");
                    CommitDatabaseToDisk();
                }
            }
        }

        /// <summary>
        /// Tries to get the source of a hash
        /// </summary>
        /// <param name="hash"></param>
        /// <param name="sourceLink"></param>
        /// <returns></returns>
        public static bool TryGetSource(string hash, out string sourceLink)
        {
            sourceLink = null;
            if (!ServiceLoaded) return false;
            return Database.TryGetValue(hash, out sourceLink);
        }
    }
}