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
    public class SFARObjectWPF : SFARObject
    {
        public ICommand RestoreCommand { get; }
        public SFARObjectWPF(string file, GameTarget target, Func<string, bool> restoreSFARCallback,
            Action startingRestoreCallback, Action<object> notifyNoLongerModifiedCallback) : base(file, target, restoreSFARCallback, startingRestoreCallback, notifyNoLongerModifiedCallback)
        {
            RestoreCommand = new GenericCommand(RestoreSFARWrapper, CanRestoreSFAR);
        }

        /// <summary>
        /// Can be used as a delegate to generate an SFARObjectWPF.
        /// </summary>
        /// <param name="file"></param>
        /// <param name="gameTarget"></param>
        /// <param name="restoresfarcallback"></param>
        /// <param name="startingrestorecallback"></param>
        /// <param name="notifynolongermodifiedcallback"></param>
        /// <returns></returns>
        public static SFARObject GenerateSFARObjectWPF(string file, GameTarget gameTarget, Func<string, bool> restoresfarcallback, Action startingrestorecallback, Action<object> notifynolongermodifiedcallback)
        {
            return new SFARObjectWPF(file, gameTarget, restoresfarcallback, startingrestorecallback, notifynolongermodifiedcallback);
        }
    }
}
