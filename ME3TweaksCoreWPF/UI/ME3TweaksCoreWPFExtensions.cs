using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Shell;
using ME3TweaksCore.Misc;

namespace ME3TweaksCoreWPF.UI
{
    public static class MTaskbarStateWPF
    {
        public static TaskbarProgressBarState ConvertTaskbarState(MTaskbarState taskbarState)
        {
            switch (taskbarState)
            {
                case MTaskbarState.None:
                    return TaskbarItemProgressState.None;
                case MTaskbarState.Indeterminate:
                    return TaskbarItemProgressState.Indeterminate;
                case MTaskbarState.Progressing:
                    return TaskbarItemProgressState.Normal;
                default:
                    throw new Exception($"MTaskBarState: Undefined conversion from {taskbarState} to WPF");
            }
        }
    }
}
