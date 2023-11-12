using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Flurl.Http;
using LegendaryExplorerCore.Gammtek.Extensions.Collections.Generic;
using LegendaryExplorerCore.Helpers;
using LegendaryExplorerCore.Misc;
using ME3TweaksCore.Diagnostics;
using ME3TweaksCore.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ME3TweaksCore.Services.FileSource
{
    public class FileSourceService
    {
        /// <summary>
        /// Database of hash to download link
        /// </summary>
        private static Dictionary<long, CaseInsensitiveDictionary<FileSourceRecord>> Database;

        /// <summary>
        /// If the FileSourceService has been initially loaded
        /// </summary>
        public static bool ServiceLoaded { get; private set; }

        /// <summary>
        /// If full serialization is occurring of JSON
        /// </summary>
        internal static bool IsFullySerializing { get; set; }

        /// <summary>
        /// Service name for logging
        /// </summary>
        private const string ServiceLoggingName = @"File Source Service";

        private static string GetLocalServiceCacheFile() => MCoreFilesystem.GetFileSourceServiceFile();

        private static void InternalLoadService(JToken serviceData = null)
        {
            Database = new Dictionary<long, CaseInsensitiveDictionary<FileSourceRecord>>();
            LoadDatabase(Database, serviceData);
        }

        private static void LoadDatabase(Dictionary<long, CaseInsensitiveDictionary<FileSourceRecord>> database, JToken serviceData = null)
        {
            // First load the local data
            LoadLocalData(database);

            // Then load the server data
            // Online data is merged into local and then committed to disk if updated
            if (serviceData != null)
            {
                try
                {
                    bool updated = false;
                    // Read service data and merge into the local database file
                    var onlineDB = serviceData.ToObject<List<FileSourceRecord>>();
                    foreach (var onlineDBRecord in onlineDB)
                    {
                        if (!Database.TryGetValue(onlineDBRecord.Size, out var filesWithSize))
                        {
                            filesWithSize = new CaseInsensitiveDictionary<FileSourceRecord>(1);
                            Database[onlineDBRecord.Size] = filesWithSize;
                            updated = true;
                        }

                        updated |= filesWithSize.TryAdd(onlineDBRecord.Hash, onlineDBRecord);
                    }

                    if (updated)
                    {
                        MLog.Information($@"Merged online {ServiceLoggingName} into local version");
                        CommitDatabaseToDisk();
                    }
                    else
                    {
                        MLog.Information($@"Local {ServiceLoggingName} is up to date with online version");
                    }
                    return;
                }
                catch (Exception ex)
                {

                    MLog.Error($@"Failed to load online {ServiceLoggingName}: {ex.Message}");
                    return;
                }
            }
        }

        private static void LoadLocalData(Dictionary<long, CaseInsensitiveDictionary<FileSourceRecord>> database)
        {
            var file = GetLocalServiceCacheFile();
            if (File.Exists(file))
            {
                try
                {
                    var db = JsonConvert.DeserializeObject<Dictionary<long, CaseInsensitiveDictionary<FileSourceRecord>>>(File.ReadAllText(file));
                    foreach (var val in db)
                    {
                        foreach (var val2 in val.Value)
                        {
                            val2.Value.Name = null; // Purge this on load
                        }
                    }
                    database.ReplaceAll(db);
                    MLog.Information($@"Loaded local {ServiceLoggingName}");
                }
                catch (Exception e)
                {
                    MLog.Error($@"Error loading local {ServiceLoggingName}: {e.Message}");
                }
            }
            else
            {
                MLog.Information($@"Loaded blank local {ServiceLoggingName}");
                database.Clear();
            }
        }

        public static void AddFileSourceEntries(CaseInsensitiveDictionary<FileSourceRecord> entries, int? telemetryKey)
        {
            bool updated = false;
            // Update the DB
            foreach (var entry in entries)
            {
                if (!Database.TryGetValue(entry.Value.Size, out var md5Map))
                {
                    md5Map = new CaseInsensitiveDictionary<FileSourceRecord>(1);
                    Database[entry.Value.Size] = md5Map;
                    updated = true;
                }

                if (md5Map.TryAdd(entry.Key, entry.Value))
                {
                    updated = true;
                }
            }

            // Serialize it back to disk
            if (updated)
            {
                CommitDatabaseToDisk();

                // Run in a task to put onto a background thread. We kind of don't care how long this takes or if it errors
                Task.Run(() =>
                {
                    // telemetrykey is used to gate this feature
                    if (telemetryKey != null)
                    {
                        var dict = new Dictionary<string, object>()
                        {
                            {@"key", telemetryKey},
                            {@"data", entries}
                        };
                        IsFullySerializing = true;
                        @"https://me3tweaks.com/modmanager/services/submithashsource".PostJsonAsync(dict);
                        //var result = "https://me3tweaks.com/modmanager/services/submithashsource".PostJsonAsync(dict).Result;
                        //var text = result.GetStringAsync().Result;
                        IsFullySerializing = false;
                    }
                }).ContinueWith(x =>
                {
                    if (x.Exception != null)
                    {

                    }
                });

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
                File.WriteAllText(GetLocalServiceCacheFile(), outText);
                MLog.Information($@"Updated local {ServiceLoggingName}");
            }
            catch (Exception e)
            {
                // bwomp bwomp
                MLog.Error($@"Error saving local {ServiceLoggingName}: {e.Message}");
            }
        }

        /// <summary>
        /// Looks up download information for a file based on its size and md5
        /// </summary>
        /// <param name="target"></param>
        /// <param name="fullfilepath"></param>
        /// <returns></returns>
        public static string GetDownloadLink(long size, string md5)
        {
            if (Database.TryGetValue(size, out var md5Map) && md5Map.TryGetValue(md5, out var link))
            {
                return link.DownloadLink;
            }
            return null;
        }

        public static bool LoadService(JToken data)
        {
            InternalLoadService(data);
            ServiceLoaded = true;
            return true;
        }

        private static object syncObj = new object();

        /// <summary>
        /// Purges the entry with the specific size and md5, then commits the file back to disk.
        /// </summary>
        /// <param name="game">The game to purge entries for</param>
        public static void PurgeEntry(long size, string md5)
        {
            lock (syncObj)
            {
                if (Database.TryGetValue(size, out var md5Map))
                {
                    if (md5Map.Remove(md5))
                    {
                        MLog.Information($@"Removed {md5} from {ServiceLoggingName}");

                        // Purge empty size branches
                        if (md5Map.Count == 0)
                        {
                            Database.Remove(size);
                        }

                        CommitDatabaseToDisk();
                    }
                }
            }
        }

        /// <summary>
        /// Tries to get the source of a size/hash
        /// </summary>
        /// <param name="hash"></param>
        /// <param name="sourceLink"></param>
        /// <returns></returns>
        public static bool TryGetSource(long size, string hash, out string sourceLink)
        {
            sourceLink = null;
            if (!ServiceLoaded) return false;
            if (Database.TryGetValue(size, out var md5Map))
            {
                // > 128MiB files are checked only on size to improve performance
                if (size >= FileSize.MebiByte * 128 && md5Map.Count == 1)
                {
                    sourceLink = md5Map.First().Value.DownloadLink;
                }
                else
                {
                    if (md5Map.TryGetValue(hash, out var fssRecord))
                    {
                        sourceLink = fssRecord.DownloadLink;
                    }
                }
            }

            return sourceLink != null;
        }
    }
}