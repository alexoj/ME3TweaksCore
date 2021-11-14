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
    class SFARObjectWPF : SFARObject
    {
        public ICommand RestoreCommand { get; }
        public SFARObjectWPF(string file, GameTarget target, Func<string, bool> restoreSFARCallback,
            Action startingRestoreCallback, Action<object> notifyNoLongerModifiedCallback) : base(file, target, restoreSFARCallback, startingRestoreCallback, notifyNoLongerModifiedCallback)
        {
            RestoreCommand = new GenericCommand(RestoreSFARWrapper, CanRestoreSFAR);
        }
    }
}
