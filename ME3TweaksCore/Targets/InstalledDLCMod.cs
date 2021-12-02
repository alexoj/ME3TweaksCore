using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using LegendaryExplorerCore.Misc;
using LegendaryExplorerCore.Packages;
using ME3TweaksCore.GameFilesystem;
using ME3TweaksCore.Helpers;
using ME3TweaksCore.Localization;
using ME3TweaksCore.Services.ThirdPartyModIdentification;
using PropertyChanged;
using Serilog;

namespace ME3TweaksCore.Targets
{
    [AddINotifyPropertyChangedInterface]
    public class InstalledDLCMod
    {
        protected string dlcFolderPath;
        public string EnableDisableText => DLCFolderName.StartsWith(@"xDLC") ? LC.GetString(LC.string_enable) : LC.GetString(LC.string_disable);
        public string EnableDisableTooltip { get; set; }
        public string ModName { get; private set; }
        public string DLCFolderName { get; private set; }
        public string DLCFolderNameString { get; private set; }
        public string InstalledBy { get; private set; }
        public string Version { get; private set; }
        public string InstallerInstanceBuild { get; private set; }


        public ObservableCollectionExtended<string> IncompatibleDLC { get; } = new ObservableCollectionExtended<string>();
        public ObservableCollectionExtended<string> ChosenInstallOptions { get; } = new ObservableCollectionExtended<string>();

        private MEGame game;

        protected Func<InstalledDLCMod, bool> deleteConfirmationCallback;
        protected Action notifyDeleted;
        private Action notifyToggled;


        

        /// <summary>
        /// Indicates that this mod was installed by ALOT Installer or Mod Manager.
        /// </summary>
        public bool InstalledByManagedSolution { get; private set; }
        public virtual void OnDLCFolderNameChanged()
        {
            dlcFolderPath = Path.Combine(Directory.GetParent(dlcFolderPath).FullName, DLCFolderName);
            parseMetaCmm(DLCFolderName.StartsWith('x'), false);
        }

        public InstalledDLCMod(string dlcFolderPath, MEGame game, Func<InstalledDLCMod, bool> deleteConfirmationCallback, Action notifyDeleted, Action notifyToggled, bool modNamePrefersTPMI)
        {
            this.dlcFolderPath = dlcFolderPath;
            this.game = game;
            var dlcFolderName = DLCFolderNameString = Path.GetFileName(dlcFolderPath);
            if (TPMIService.TryGetModInfo(game, dlcFolderName.TrimStart('x'), out var tpmi))
            {
                ModName = tpmi.modname;
            }
            else
            {
                ModName = dlcFolderName;
            }

            DLCFolderName = dlcFolderName;
            this.deleteConfirmationCallback = deleteConfirmationCallback;
            this.notifyDeleted = notifyDeleted;
            this.notifyToggled = notifyToggled;
        }

        private void parseMetaCmm(bool disabled, bool modNamePrefersTPMI)
        {
            DLCFolderNameString = DLCFolderName.TrimStart('x'); //this string is not to show M3L.GetString(M3L.string_disabled)
            var metaFile = Path.Combine(dlcFolderPath, @"_metacmm.txt");
            if (File.Exists(metaFile))
            {
                InstalledByManagedSolution = true;
                InstalledBy = LC.GetString(LC.string_installedByModManager); //Default value when finding metacmm.
                MetaCMM mcmm = new MetaCMM(metaFile);
                if (DLCFolderNameString != ModName && mcmm.ModName != ModName)
                {
                    DLCFolderNameString += $@" ({ModName})";
                    if (!modNamePrefersTPMI || ModName == null)
                    {
                        ModName = mcmm.ModName;
                    }
                }

                ModName = mcmm.ModName;
                Version = mcmm.Version;
                InstallerInstanceBuild = mcmm.InstalledBy;
                if (int.TryParse(InstallerInstanceBuild, out var _))
                {
                    InstalledBy = LC.GetString(LC.string_installedByModManager) + @" Build " + InstallerInstanceBuild; // Installed by Mod Manager Build X. Doens't need localized
                }
                else
                {
                    InstalledBy = LC.GetString(LC.string_interp_installedByX, InstallerInstanceBuild);
                }

                // MetaCMM Extended
                if (mcmm.OptionsSelectedAtInstallTime != null)
                {
                    ChosenInstallOptions.ReplaceAll(mcmm.OptionsSelectedAtInstallTime);
                }
                if (mcmm.IncompatibleDLC != null)
                {
                    IncompatibleDLC.ReplaceAll(mcmm.IncompatibleDLC);
                }
            }
            else
            {
                InstalledBy = LC.GetString(LC.string_notInstalledByModManager);
            }
            if (disabled)
            {
                DLCFolderNameString += @" - " + LC.GetString(LC.string_disabled);
            }
        }

        protected void ToggleDLC()
        {
            var source = dlcFolderPath;
            var dlcdir = Directory.GetParent(dlcFolderPath).FullName;
            var isBecomingDisabled = DLCFolderName.StartsWith(@"DLC"); //about to change to xDLC, so it's becoming disabled
            var newdlcname = DLCFolderName.StartsWith(@"xDLC") ? DLCFolderName.TrimStart('x') : @"x" + DLCFolderName;
            var target = Path.Combine(dlcdir, newdlcname);
            try
            {
                Directory.Move(source, target);
                DLCFolderName = newdlcname;
                dlcFolderPath = target;
                EnableDisableTooltip = LC.GetString(isBecomingDisabled ? LC.string_tooltip_enableDLC : LC.string_tooltip_disableDLC);
                notifyToggled?.Invoke();
            }
            catch (Exception e)
            {
                Log.Error(@"Unable to toggle DLC: " + e.Message);
            }
            //TriggerPropertyChangedFor(nameof(DLCFolderName));
        }

        protected bool CanToggleDLC() => (game is MEGame.ME3 || game.IsLEGame() || DLCFolderName.StartsWith('x')) && !MUtilities.IsGameRunning(game);

        public bool EnableDisableVisible => game is MEGame.ME3 || game.IsLEGame() || DLCFolderName.StartsWith('x');
        protected bool CanDeleteDLCMod() => !MUtilities.IsGameRunning(game);

        protected virtual void DeleteDLCMod()
        {
            var confirmDelete = deleteConfirmationCallback?.Invoke(this);
            if (confirmDelete.HasValue && confirmDelete.Value)
            {
                Log.Information(@"Deleting DLC mod from target: " + dlcFolderPath);
                try
                {
                    MUtilities.DeleteFilesAndFoldersRecursively(dlcFolderPath);
                    notifyDeleted?.Invoke();
                }
                catch (Exception e)
                {
                    Log.Error($@"Error deleting DLC mod: {e.Message}");
                    // Todo: Show a dialog to the user
                }
            }
        }

        public void ClearHandlers()
        {
            deleteConfirmationCallback = null;
            notifyDeleted = null;
        }
    }
}
