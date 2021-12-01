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
        private const string LoggingPrefix = "[ME3TWEAKSCORE] ";

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

        /// <summary>
        /// Logs a string to the log. You can specify a prefix or a boolean that is checked (for making calls easier)
        /// </summary>
        /// <param name="message"></param>
        /// <param name="prefix"></param>
        /// <param name="shouldLog"></param>
        internal static void Fatal(string message, string prefix = null, bool shouldLog = true)
        {
            if (shouldLog)
            {
                Log.Fatal($"{prefix ?? LoggingPrefix}{message}");
            }
        }

        /// <summary>
        /// Calls Log.CloseAndFlush(). This is here just so everything routes through MLog.
        /// </summary>
        public static void CloseAndFlush()
        {
            Log.CloseAndFlush();

        }

        public static void Exception(Exception exception, string preMessage, bool fatal = false)
        {
            Log.Error($"{LoggingPrefix}{preMessage}");

            // Log exception
            while (exception != null)
            {
                var line1 = exception.GetType().Name + ": " + exception.Message;
                foreach (var line in line1.Split("\n"))
                {
                    if (fatal)
                        Log.Fatal(LoggingPrefix + line);
                    else
                        Log.Error(LoggingPrefix + line);

                }

                if (exception.StackTrace != null)
                {
                    foreach (var line in exception.StackTrace.Split("\n"))
                    {
                        if (fatal)
                            Log.Fatal(LoggingPrefix + line);
                        else
                            Log.Error(LoggingPrefix + line);
                    }
                }

                exception = exception.InnerException;
            }
        }

    }
}
