using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using ME3TweaksCore.Targets;
using ME3TweaksCoreWPF.UI;

namespace ME3TweaksCoreWPF.Targets
{
    public class ModifiedFileObjectWPF : ModifiedFileObject
    {
        public ICommand RestoreCommand { get; }
        public bool Restoring { get; set; }

        public ModifiedFileObjectWPF(string filePath, GameTarget target,
            Func<string, bool> restoreBasegamefileConfirmationCallback,
            Action notifyRestoringFileCallback,
            Action<object> notifyRestoredCallback) : base(filePath, target, restoreBasegamefileConfirmationCallback, notifyRestoredCallback)
        {
            RestoreCommand = new GenericCommand(RestoreFileWrapper, CanRestoreFile);
        }
    }
}
