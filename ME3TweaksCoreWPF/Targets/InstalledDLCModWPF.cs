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
        // This needs implemented in a application specific way.
        //private static readonly SolidColorBrush DisabledBrushLightMode = new SolidColorBrush(Color.FromArgb(0xff, 232, 26, 26));
        //private static readonly SolidColorBrush DisabledBrushDarkMode = new SolidColorBrush(Color.FromArgb(0xff, 247, 88, 77));

        //[DependsOn(nameof(DLCFolderName))]
        //public SolidColorBrush TextColor
        //{
        //    get
        //    {
        //        if (DLCFolderName.StartsWith('x'))
        //        {
        //            return Settings.DarkTheme ? DisabledBrushDarkMode : DisabledBrushLightMode;
        //        }
        //        return MediaTypeNames.Application.Current.FindResource(AdonisUI.Brushes.ForegroundBrush) as SolidColorBrush;
        //    }
        //}

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
    }
}
