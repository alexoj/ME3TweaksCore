﻿using System;
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
        private static Action<string, Dictionary<string, string>> TrackEventCallback { get; set; }

        public void SetEventCallback(Action<string, Dictionary<string, string>> trackEventCallback)
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


        /* // Example callback
                    var attachments = new List<ErrorAttachmentLog>();
                    string log = LogCollector.CollectLatestLog(true);
                    if (log != null && log.Length < FileSize.MebiByte * 7)
                    {
                        attachments.Add(ErrorAttachmentLog.AttachmentWithText(log, @"applog.txt"));
                    }
                    Crashes.TrackError(e, , attachments.ToArray());
         */
        private static Action<Exception, Dictionary<string, string>> UploadErrorLogCallback { get; set; }

        public static void SetUploadErrorLogCallback(Action<Exception, Dictionary<string, string>> uploadErrorLogCallback)
        {
            UploadErrorLogCallback = uploadErrorLogCallback;
        }

        /// <summary>
        /// Instructs the calling application to trigger a log upload because an error has occurred that should be investigated.
        /// </summary>
        /// <param name="exception"></param>
        /// <param name="relevantInfo"></param>
        internal static void UploadErrorLog(Exception exception, Dictionary<string, string> relevantInfo)
        {
            UploadErrorLogCallback?.Invoke(exception, relevantInfo);
        }
    }
}
