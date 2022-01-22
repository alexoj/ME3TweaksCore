using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ME3TweaksCore.NativeMods
{
    /// <summary>
    /// Contains (some, not all) ASI Update Group IDs that can be used to request install of an ASI.
    /// </summary>
    public static class ASIModIDs
    {
        // ASI IDS---------------
        // This is not comprehensive list. Just here for convenience.

        public static readonly int ME1_DLC_MOD_ENABLER = 16;

        public static readonly int ME3_AUTOTOC = 9;
        public static readonly int ME3_LOGGER = 8;

        public static readonly int LE1_DLC_MOD_ENABLER = 32;
        public static readonly int LE1_AUTOTOC = 29;
        //public static readonly int LE1_DEBUGLOGGER_USER = 70;
        public static readonly int LE1_DEBUGLOGGER_DEV = 70;

        public static readonly int LE2_AUTOTOC = 30;
        public static readonly int LE2_DEBUGLOGGER_DEV = 71;

        public static readonly int LE3_AUTOTOC = 31;
        public static readonly int LE3_DEBUGLOGGER_DEV = 72;

        // ---------------------
    }
}
