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

        public ModifiedFileObjectWPF(string filePath, GameTarget target,
            Func<string, bool> restoreBasegamefileConfirmationCallback,
            Action notifyRestoringFileCallback,
            Action<object> notifyRestoredCallback) : base(filePath, target, restoreBasegamefileConfirmationCallback, notifyRestoringFileCallback, notifyRestoredCallback)
        {
            RestoreCommand = new GenericCommand(RestoreFileWrapper, CanRestoreFile);
        }

        /// <summary>
        /// Can be used to generate a ModifiedFileObjectWPF, for use in delegates.
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="target"></param>
        /// <param name="restoreBasegamefileConfirmationCallback"></param>
        /// <param name="notifyRestoringFileCallback"></param>
        /// <param name="notifyRestoredCallback"></param>
        /// <returns></returns>
        public static ModifiedFileObjectWPF GenerateModifiedFileObjectWPF(string filePath, GameTarget target, Func<string, bool> restoreBasegamefileConfirmationCallback, Action notifyRestoringFileCallback, Action<object> notifyRestoredCallback)
        {
            return new ModifiedFileObjectWPF(filePath, target, restoreBasegamefileConfirmationCallback, notifyRestoringFileCallback, notifyRestoredCallback);
        }
    }
}
