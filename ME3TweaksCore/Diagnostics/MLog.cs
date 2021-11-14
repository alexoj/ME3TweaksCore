using System;
using LegendaryExplorerCore.Helpers;
using Serilog;

namespace ME3TweaksCore.Diagnostics
{
    /// <summary>
    /// Logger for ME3Tweaks software. Pass an ILogger into this class to set the logger to sync with your external software.
    /// </summary>
    public class MLog
    {
        public void SetLogger(ILogger logger)
        {
            Log.Logger = logger;
        }

        /// <summary>
        /// Logging prefix for ME3TWEAKSCORE logs.
        /// </summary>
        private const string LoggingPrefix = "[ME3TWEAKSCORE]";

        /// <summary>
        /// Logs a string to the log. You can specify a prefix or a boolean that is checked (for making calls easier)
        /// </summary>
        /// <param name="message"></param>
        /// <param name="prefix"></param>
        /// <param name="shouldLog"></param>
        internal static void Information(string message, bool shouldLog = true)
        {
            if (shouldLog)
            {
                Log.Information($"{LoggingPrefix}{message}");
            }
        }

        /// <summary>
        /// Logs a string to the log. You can specify a prefix or a boolean that is checked (for making calls easier)
        /// </summary>
        /// <param name="message"></param>
        /// <param name="prefix"></param>
        /// <param name="shouldLog"></param>
        internal static void Warning(string message, bool shouldLog = true)
        {
            if (shouldLog)
            {
                Log.Warning($"{LoggingPrefix}{message}");
            }
        }

        /// <summary>
        /// Logs a string to the log. You can specify a prefix or a boolean that is checked (for making calls easier)
        /// </summary>
        /// <param name="message"></param>
        /// <param name="prefix"></param>
        /// <param name="shouldLog"></param>
        internal static void Error(string message, string prefix = null, bool shouldLog = true)
        {
            if (shouldLog)
            {
                Log.Error($"{prefix ?? LoggingPrefix}{message}");
            }
        }

        /// <summary>
        /// Logs a string to the log. You can specify a prefix or a boolean that is checked (for making calls easier)
        /// </summary>
        /// <param name="message"></param>
        /// <param name="prefix"></param>
        /// <param name="shouldLog"></param>
        internal static void Error(Exception ex, bool shouldLog = true)
        {
            if (shouldLog)
            {
                Log.Error($"{LoggingPrefix}{ex.FlattenException()}");
            }
        }
    }
}
