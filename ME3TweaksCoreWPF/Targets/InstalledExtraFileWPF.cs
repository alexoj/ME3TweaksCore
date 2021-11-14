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
    }
}
