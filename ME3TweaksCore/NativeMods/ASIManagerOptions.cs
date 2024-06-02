using System.ComponentModel;

namespace ME3TweaksCore.NativeMods
{
    /// <summary>
    /// Configuration options for ASI Manager
    /// </summary>
    public class ASIManagerOptions
    {
        /// <summary>
        /// If Beta ASIs should be available
        /// </summary>
        public bool BetaMode { get; set; }

        /// <summary>
        /// If ASIs for developers should be shown
        /// </summary>
        public bool DevMode { get; set; }
    }
}
