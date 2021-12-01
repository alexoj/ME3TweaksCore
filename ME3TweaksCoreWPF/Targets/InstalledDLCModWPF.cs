using System;
using System.Windows.Input;
using LegendaryExplorerCore.Packages;
using ME3TweaksCore.Helpers;
using ME3TweaksCore.Targets;
using ME3TweaksCoreWPF.UI;
using PropertyChanged;
using Serilog;

namespace ME3TweaksCoreWPF.Targets
{
    [AddINotifyPropertyChangedInterface]
    public class InstalledDLCModWPF : InstalledDLCMod
    {
        public GenericCommand EnableDisableCommand { get; set; }
        public GenericCommand DeleteCommand { get; set; }


        public InstalledDLCModWPF(string dlcFolderPath, MEGame game, Func<InstalledDLCMod, bool> deleteConfirmationCallback, Action notifyDeleted, Action notifyToggled, bool modNamePrefersTPMI) : base(dlcFolderPath, game, deleteConfirmationCallback, notifyDeleted, notifyToggled, modNamePrefersTPMI)
        {
            DeleteCommand = new GenericCommand(DeleteDLCMod, CanDeleteDLCMod);
            EnableDisableCommand = new GenericCommand(ToggleDLC, CanToggleDLC);
        }

        /// <summary>
        /// WPF implementation of DeleteDLCMod()
        /// </summary>
        protected override void DeleteDLCMod()
        {
            bool? holdingShift = Keyboard.Modifiers == ModifierKeys.Shift;
            if (!holdingShift.Value) holdingShift = null;
            var confirmDelete = holdingShift ?? deleteConfirmationCallback?.Invoke(this);
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


        /// <summary>
        /// Generates the WPF version of an InstalledDLCModObject.
        /// </summary>
        /// <param name="dlcfolderpath"></param>
        /// <param name="game"></param>
        /// <param name="deleteconfirmationcallback"></param>
        /// <param name="notifydeleted"></param>
        /// <param name="notifytoggled"></param>
        /// <param name="modnamepreferstpmi"></param>
        /// <returns></returns>
        private static InstalledDLCMod GenerateInstalledDLCModObject(string dlcfolderpath, MEGame game, Func<InstalledDLCMod, bool> deleteconfirmationcallback, Action notifydeleted, Action notifytoggled, bool modnamepreferstpmi)
        {
            return new InstalledDLCModWPF(dlcfolderpath, game, deleteconfirmationcallback, notifydeleted, notifytoggled, modnamepreferstpmi);
        }
    }
}
