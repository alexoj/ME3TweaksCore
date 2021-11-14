using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ME3TweaksCore.Helpers
{
    /// <summary>
    /// Interposer class for calling telemetry methods from within the core library
    /// </summary>
    public class TelemetryInterposer
    {
        /// <summary>
        /// Callback to invoke to outer library if it uses telemetry
        /// </summary>
        private Action<string, Dictionary<string, string>> TrackEventCallback { get; set; }

        public void SetEventCallback(Action<string, Dictionary<string,string>> trackEventCallback)
        {
            TrackEventCallback = trackEventCallback;
        }


        public void SetErrorCallback()
        {

        }


        public static void TrackEvent(string eventName, Dictionary<string, string> data)
        {
            TrackEventCallback?.Invoke(eventName, data);
        }
    }
}
