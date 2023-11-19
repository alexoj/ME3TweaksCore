using LegendaryExplorerCore.Coalesced.Config;
using ME3TweaksCore.Targets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LegendaryExplorerCore.Packages;
using ME3TweaksCore.GameFilesystem;

namespace ME3TweaksCore.Config
{
    public class M3CConfigTools
    {
        /// <summary>
        /// Builds a combined config bundle representing the final set of the files on disk for the specified target
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public static ConfigAssetBundle GetMergedBundle(GameTarget target)
        {
            return ConfigTools.GetMergedBundle(target.Game, target.TargetPath);
        }
    }
}
