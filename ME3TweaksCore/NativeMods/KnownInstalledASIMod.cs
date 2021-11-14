using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LegendaryExplorerCore.Packages;

namespace ME3TweaksCore.NativeMods
{
    /// <summary>
    /// ASI file that is mapped to a known ASI file
    /// </summary>
    public class KnownInstalledASIMod : InstalledASIMod
    {
        public KnownInstalledASIMod(string filepath, string hash, MEGame game, ASIModVersion mappedVersion) : base(filepath, hash, game)
        {
            AssociatedManifestItem = mappedVersion;
        }

        /// <summary>
        /// The manifest version information about this installed ASI mod
        /// </summary>
        public ASIModVersion AssociatedManifestItem { get; set; }

        /// <summary>
        /// If this installed ASI mod is outdated
        /// </summary>
        public bool Outdated => AssociatedManifestItem.OwningMod.Versions.Last() != AssociatedManifestItem;

        public string InstallStatus => Outdated ? M3L.GetString(M3L.string_outdatedVersionInstalled) : M3L.GetString(M3L.string_installed);
    }
}
