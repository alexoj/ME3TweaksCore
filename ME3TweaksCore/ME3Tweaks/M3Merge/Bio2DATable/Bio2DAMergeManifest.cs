using System.Collections.Generic;
using Newtonsoft.Json;

namespace ME3TweaksCore.ME3Tweaks.M3Merge.Bio2DATable
{
    /// <summary>
    /// Definition of a single package file Bio2DA Merge.
    /// </summary>
    public class Bio2DAMergeManifest
    {
        /// <summary>
        /// Comment for this entry. We do nothing with this except for reading it in the event we ever serialize this object back to disk.
        /// </summary>
        [JsonProperty(@"comment")]
        public string Comment { get; set; }

        /// <summary>
        /// Name of the basegame package file to merge into
        /// </summary>
        [JsonProperty(@"packagefile")]
        public string GamePackageFile { get; set; }

        /// <summary>
        /// File name of the package containing tables to merge - in the mod folder
        /// </summary>
        [JsonProperty(@"mergepackagefile")]
        public string ModPackageFile { get; set; }

        /// <summary>
        /// List of table exports to merge
        /// </summary>
        [JsonProperty(@"mergetables")]
        public List<string> ModTables { get; set; }
    }
}
