using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using LegendaryExplorerCore.Packages;
using ME3TweaksCore.NativeMods;

namespace ME3TweaksCoreWPF.NativeMods
{
    /// <summary>
    /// ASI file that is mapped to a known ASI file (WPF extensions). This class mirrors the non WPF version but does NOT subclass its subclasses!
    /// </summary>
    public class KnownInstalledASIModWPF : InstalledASIModWPF
    {
        private static Brush installedBrush = new SolidColorBrush(Color.FromArgb(0x33, 0, 0xFF, 0));
        private static Brush outdatedBrush = new SolidColorBrush(Color.FromArgb(0x33, 0xFF, 0xFF, 0));

        public KnownInstalledASIModWPF(string filepath, string hash, MEGame game, ASIModVersion mappedVersion) : base(filepath, hash, game)
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

        public override Brush BackgroundColor => Outdated ? outdatedBrush : installedBrush;
    }
}
