using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ME3TweaksCore.Services.FileSource
{
    /// <summary>
    /// Describes a single file source
    /// </summary>
    public class FileSourceRecord
    {
        [JsonProperty(@"hash")]
        public string Hash { get; set; }

        [JsonProperty(@"size")]
        public long Size { get; set; }

        [JsonProperty(@"downloadlink")]
        public string DownloadLink { get; set; }

        // The server will not reply with this information to reduce size, it is for serialization only for debugging
        [JsonProperty(@"name")]
        public string Name { get; set; }

        #region NEWTONSOFT STUFF
        public virtual bool ShouldSerializeName()
        {
            return FileSourceService.IsFullySerializing;
        }
        #endregion
    }
}
