using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ME3TweaksCore.Helpers
{
    /// <summary>
    /// Class that gets information about the executable that is consuming this library
    /// </summary>
    public class MLibraryConsumer
    {

        /// <summary>
        /// Returns the running application version information
        /// </summary>
        /// <returns></returns>
        public static Version GetAppVersion() => System.Reflection.Assembly.GetEntryAssembly().GetName().Version;

        /// <summary>
        /// Gets the executable path that is hosting this library.
        /// </summary>
        /// <returns></returns>
        public static string GetExecutablePath() => Process.GetCurrentProcess().MainModule.FileName;

        /// <summary>
        /// Gets the version information for the ALOT Installer Core Library.
        /// </summary>
        /// <returns></returns>
        public static Version GetLibraryVersion() => Assembly.GetExecutingAssembly().GetName().Version;

        /// <summary>
        /// Returns the hosting processes' name, without extension
        /// </summary>
        /// <returns></returns>
#if (!WINDOWS && DEBUG)
        // running process will be 'dotnet' in this mode
        public static string GetHostingProcessname() => "ME3TweaksCore";
#else
        public static string GetHostingProcessname() => Path.GetFileNameWithoutExtension(Process.GetCurrentProcess().MainModule.ModuleName);
#endif
    }
}
