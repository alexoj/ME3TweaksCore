using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ME3TweaksCore.Misc
{
    public enum MTaskbarState
    {
        /// <summary>
        /// Show nothing on the taskbar.
        /// </summary>
        None,
        /// <summary>
        /// Show a progressbar on the taskbar.
        /// </summary>
        Progressing,
        /// <summary>
        /// Show an indeterminate effect on the taskbar.
        /// </summary>
        Indeterminate
    }
}
