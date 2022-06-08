using System.Linq;
using ME3TweaksCore.Localization;

namespace ME3TweaksCore.NativeMods.Interfaces
{
    /// <summary>
    /// Interface for all KnownInstalledASI implementations (as they have to fork for subclassing)
    /// </summary>
    public interface IKnownInstalledASIMod : IInstalledASIMod
    {
        /// <summary>
        /// The manifest version information about this installed ASI mod
        /// </summary>
        public ASIModVersion AssociatedManifestItem { get; set; }

        /// <summary>
        /// If this installed ASI mod is outdated
        /// </summary>
        public bool Outdated => AssociatedManifestItem.OwningMod.LatestVersion.Version > AssociatedManifestItem.Version;

        /// <summary>
        /// The installation status string of the ASI
        /// </summary>
        public string InstallStatus => Outdated ? LC.GetString(LC.string_outdatedVersionInstalled) : LC.GetString(LC.string_installed);
    }
}
