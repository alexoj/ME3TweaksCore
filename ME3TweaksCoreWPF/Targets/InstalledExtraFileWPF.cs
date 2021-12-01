using System;
using System.Windows.Input;
using LegendaryExplorerCore.Packages;
using ME3TweaksCore.Targets;
using ME3TweaksCoreWPF.UI;
using PropertyChanged;

namespace ME3TweaksCoreWPF.Targets
{
    [AddINotifyPropertyChangedInterface]
    public class InstalledExtraFileWPF : InstalledExtraFile
    {
        public ICommand DeleteCommand { get; }

        public InstalledExtraFileWPF(string filepath, EFileType type, MEGame game, Action<InstalledExtraFile> notifyDeleted = null) : base(filepath, type, game, notifyDeleted)
        {
            DeleteCommand = new GenericCommand(DeleteExtraFile, CanDeleteFile);
        }

        /// <summary>
        /// Can be used as a delegate to generate an InstalledExtraFileWPF object.
        /// </summary>
        /// <param name="dlcFolderPath"></param>
        /// <param name="game"></param>
        /// <param name="deleteConfirmationCallback"></param>
        /// <param name="notifyDeleted"></param>
        /// <param name="notifyToggled"></param>
        /// <param name="modNamePrefersTPMI"></param>
        /// <returns></returns>
        public static InstalledExtraFileWPF GenerateInstalledExtraFileWPF(string filepath, EFileType type, MEGame game, Action<InstalledExtraFile> notifyDeleted = null)
        {
            return new InstalledExtraFileWPF(filepath, type, game, notifyDeleted);
        }
    }
}
