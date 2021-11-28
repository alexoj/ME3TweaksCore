using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ME3TweaksCore.Diagnostics;
using ME3TweaksCore.Helpers;
using ME3TweaksCore.Services;
using Serilog;

namespace ME3TweaksCore
{
    /// <summary>
    /// Class containing variables and callbacks that can be assigned to to pass to the ME3TweaksCore InitLib method.
    /// </summary>
    public class ME3TweaksCoreLibInitPackage
    {
        /// <summary>
        /// Specifies if auxillary services such as BasegameFileIdentificationService, ThirdPartyIdentificationService, etc, should initialize with the library. Setting this to false
        /// means that the consuming library will manually initialize them.
        /// </summary>
        public bool LoadAuxillaryServices { get; set; } = true;

        /// <summary>
        /// The run on ui thread delegate. If you are not using WPF, this should just return a thread.
        /// </summary>
        [DisallowNull]
        public Action<Action> RunOnUiThreadDelegate { get; init; }
        
        /// <summary>
        /// Delegate to create a logger. Used when a log collection takes place and the logger must be restarted.
        /// </summary>
        public Func<ILogger> CreateLogger { get; init; }

        /// <summary>
        /// Function to invoke to test if we can fetch online content. This method should be throttled (for example, once per day) to prevent server overload or having the web host blacklist the client IP.
        /// </summary>
        public Func<bool> CanFetchContentThrottleCheck { get; init; }


        // TELEMETRY CALLBACKS

        /// <summary>
        /// Delegate to call when the internal library wants to track that an event has occurred.
        /// </summary>
        public Action<string, Dictionary<string, string>> TrackEventCallback { get; set; }
        /// <summary>
        /// Delegate to call when the internal library wants to track that an error has occurred.
        /// </summary>
        public Action<Exception, Dictionary<string, string>> TrackErrorCallback { get; set; }
        /// <summary>
        /// Delegate to call when the internal library wants to track that an error has occurred and a log should also be included.
        /// </summary>
        public Action<Exception, Dictionary<string, string>> UploadErrorLogCallback { get; set; }
        /// <summary>
        /// Called by LegendaryExplorerCore when a package fails to save.
        /// </summary>
        public Action<string> LECPackageSaveFailedCallback { get; set; }


        /// <summary>
        /// Installs the callbacks specified in this package into ME3TweaksCore.
        /// </summary>
        internal void InstallInitPackage()
        {
            LogCollector.CreateLogger = CreateLogger;
            Log.Logger ??= LogCollector.CreateLogger?.Invoke();

            ME3TweaksCoreLib.RunOnUIThread = RunOnUiThreadDelegate;

            if (CanFetchContentThrottleCheck != null)
            {
                MOnlineContent.CanFetchContentThrottleCheck = CanFetchContentThrottleCheck;
            }
            
            TelemetryInterposer.SetErrorCallback(TrackErrorCallback);
            TelemetryInterposer.SetUploadErrorLogCallback(UploadErrorLogCallback);
            TelemetryInterposer.SetEventCallback(TrackEventCallback);
        }
    }
}
