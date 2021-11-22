using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using LegendaryExplorerCore;
using ME3TweaksCore.Diagnostics;
using ME3TweaksCore.Helpers;
using ME3TweaksCore.Services.Backup;
using Serilog;

namespace ME3TweaksCore
{
    /// <summary>
    /// Boot class for the ME3TweaksCore library. You must call Initialize() before using this library to ensure dependencies are loaded.
    /// </summary>
    public static class ME3TweaksCoreLib
    {
        /// <summary>
        /// If the library has already been initialized.
        /// </summary>
        private static bool Initialized;

        public static Action<Action> RunOnUIThread;

        public static Version MIN_SUPPORTED_OS => new Version();

        /// <summary>
        /// The CoreLibVersion version
        /// </summary>
        public static Version CoreLibVersion => Assembly.GetExecutingAssembly().GetName().Version;
            
        /// <summary>
        /// The CoreLibrary version, in Human Readable form.
        /// </summary>
        public static string CoreLibVersionHR => $"ME3TweaksCore {CoreLibVersion}"; // Needs checked this outputs proper string.

        /// <summary>
        /// Initial initialization function for the library. You must call this function before using the library, otherwise it may not reliably work.
        /// </summary>
        /// <param name="createLogger"></param>
        public static void Initialize(Action<Action> RunOnUiThreadDelegate, Func<ILogger> createLogger = null)
        {
            if (Initialized)
            {
                return; // Already initialized.
            }

            // Initialize the logger if it is not already initialized
            LogCollector.CreateLogger = createLogger;
            Log.Logger ??= LogCollector.CreateLogger?.Invoke();

            MLog.Information($@"Initializing ME3TweaksCore library {MLibraryConsumer.GetLibraryVersion()}");

            // Load Legendary Explorer Core.
            LegendaryExplorerCoreLib.InitLib(null, logger: Log.Logger);

            // Load our library
            RunOnUIThread = RunOnUiThreadDelegate;
            MUtilities.DeleteFilesAndFoldersRecursively(MCoreFilesystem.GetTempDirectory(), deleteDirectoryItself: false); // Clear temp but don't delete the directory itself

            BackupService.InitBackupService(RunOnUIThread);

            Initialized = true;
        }
    }
}
