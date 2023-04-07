using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LegendaryExplorerCore.Packages;
using ME3TweaksCore.GameFilesystem;
using ME3TweaksCore.Targets;

namespace ME3TweaksCore.Config
{
    /// <summary>
    /// Utilities for working with configuration files in ME games
    /// </summary>
    public static class ConfigTools
    {

        /// <summary>
        /// Builds a combined config bundle representing the final set of the files on disk for the specified target
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public static ConfigAssetBundle GetMergedBundle(GameTarget target)
        {
            ConfigAssetBundle b = ConfigAssetBundle.FromSingleFile(target.Game, target.GetCoalescedPath());
            var dlcPath = target.GetDLCPath();
            var dlc = target.GetInstalledDLCByMountPriority();

            foreach (var v in dlc)
            {
                var dlcBundle = ConfigAssetBundle.FromDLCFolder(target.Game, Path.Combine(dlcPath, v, target.Game.CookedDirName()), v);
                dlcBundle.MergeInto(b);
            }
            b.CommitAssets(@"B:\out.bin");
            return b;
        }
    }
}
