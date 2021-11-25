using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ME3TweaksCore.Misc;
using Microsoft.WindowsAPICodePack.Taskbar;

namespace ME3TweaksCoreWPF.UI
{
    public static class MTaskbarStateWPF
    {
        public static TaskbarProgressBarState ConvertTaskbarState(MTaskbarState taskbarState)
        {
            switch (taskbarState)
            {
                case MTaskbarState.None:
                    return TaskbarProgressBarState.NoProgress;
                case MTaskbarState.Indeterminate:
                    return TaskbarProgressBarState.Indeterminate;
                case MTaskbarState.Progressing:
                    return TaskbarProgressBarState.Normal;
                default:
                    throw new Exception($"MTaskBarState: Undefined conversion from {taskbarState} to WPF");
            }
        }
    }
}
