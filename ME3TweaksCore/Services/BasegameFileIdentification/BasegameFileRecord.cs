using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LegendaryExplorerCore.Gammtek.Extensions;
using LegendaryExplorerCore.Packages;
using ME3TweaksCore.Helpers;
using ME3TweaksCore.Targets;

namespace ME3TweaksCore.Services.BasegameFileIdentification
{
    /// <summary>
    /// Information that can be used to identify the source of the mod for a basegame file change.
    /// </summary>
    public class BasegameFileRecord
    {
        public string file { get; set; }
        public string hash { get; set; }
        public string source { get; set; }
        public string game { get; set; }
        public int size { get; set; }
        public BasegameFileRecord() { }
        public BasegameFileRecord(string relativePathToRoot, int size, MEGame game, string humanName, string md5)
        {
            this.file = relativePathToRoot;
            this.hash = md5 ?? MUtilities.CalculateMD5(relativePathToRoot);
            this.game = game.ToGameNum().ToString(); // due to how json serializes stuff we have to convert it here.
            this.size = size;
            this.source = humanName;
        }
    }
}
