using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using LegendaryExplorerCore.Packages;
using ME3TweaksCore;
using ME3TweaksCore.Localization;
using ME3TweaksCore.Targets;
using ME3TweaksCoreWPF.Targets;

namespace ME3TweaksCoreWPF
{
    /// <summary>
    /// WPF extension class to the ME3TweaksCore GameTarget class that provides information about an installation of a game.
    /// </summary>
    public class GameTargetWPF : GameTarget
    {
        public GameTargetWPF(MEGame game, string targetRootPath, bool currentRegistryActive, bool isCustomOption = false, bool isTest = false, bool skipInit = false) : base(game, targetRootPath, currentRegistryActive, isCustomOption, isTest, skipInit)
        {

        }

        /// <summary>
        /// View for filtering modified basegame files
        /// </summary>
        public ICollectionView ModifiedBasegameFilesView => CollectionViewSource.GetDefaultView(ModifiedBasegameFiles);


        /// <summary>
        /// Determines if this gametarget can be chosen in dropdowns
        /// </summary>
        public bool Selectable { get; set; } = true;

        public string TargetBootIcon
        {
            get
            {
                if (IsCustomOption) return null; // No icon as this is not actual game
                if (GameSource == null) return @"/images/unknown.png";
                if (GameSource.Contains(@"Steam")) return @"/images/steam.png"; //higher priority than Origin in icon will make the Steam/Origin mix work
                if (GameSource.Contains(@"Origin")) return @"/images/origin.png";
                if (GameSource.Contains(@"DVD")) return @"/images/dvd.png";
                return @"/images/unknown.png";
            }
        }

        public string RemoveTargetTooltipText
        {
            get
            {
                if (RegistryActive) return LC.GetString(LC.string_dialog_cannotRemoveActiveTarget);
                return LC.GetString(LC.string_tooltip_removeTargetFromM3);
            }
        }

        /// <summary>
        /// Call this when the modified object lists are no longer necessary. In WPF this needs to be overridden to run on dispatcher
        /// </summary>
        public override void DumpModifiedFilesFromMemory()
        {
            //Some commands are made from a background thread, which means this might not be called from dispatcher
            ME3TweaksCoreLib.RunOnUIThread(()=>
            {
                ModifiedBasegameFiles.ClearEx();
                ModifiedSFARFiles.ClearEx();
                foreach (var uiInstalledDlcMod in UIInstalledDLCMods)
                {
                    uiInstalledDlcMod.ClearHandlers();
                }

                UIInstalledDLCMods.ClearEx();
            });
        }

        public override string ValidateTarget(bool ignoreCmmVanilla = false)
        {
            if (!Selectable)
                return null; // Don't bother with this
            return base.ValidateTarget(ignoreCmmVanilla);
        }
    }
}