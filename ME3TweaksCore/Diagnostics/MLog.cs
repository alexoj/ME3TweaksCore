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
        /// <summary>
        /// Logging prefix for ME3TWEAKSCORE logs.
        /// </summary>
        private const string InternalLoggingPrefix = @"ME3TWEAKSCORE";

        /// <summary>
        /// Denotes a new session in the log
        /// </summary>
        /// <param name="customPrefix"></param>
        public static void LogSessionStart(string customPrefix = InternalLoggingPrefix)
        {
            Log.Information($@"[{customPrefix}] ===========================================================================");
        }

        /// <summary>
        /// Logs a string to the log. You can specify a boolean for log checking (for making calls easier)
        /// </summary>
        /// <param name="message"></param>
        /// <param name="prefix"></param>
        /// <param name="shouldLog"></param>
        public static void Information(string message, bool shouldLog = true, string customPrefix = InternalLoggingPrefix)
        {
            if (shouldLog)
            {
                Log.Information($@"[{customPrefix}] {message}");
            }
        }

        /// <summary>
        /// Logs a string to the log. You can specify a boolean for log checking (for making calls easier)
        /// </summary>
        /// <param name="message"></param>
        /// <param name="shouldLog"></param>
        public static void Warning(string message, bool shouldLog = true, string customPrefix = InternalLoggingPrefix)
        {
            if (shouldLog)
            {
                Log.Warning($@"[{customPrefix}] {message}");
            }
        }

        /// <summary>
        /// Logs a string to the log. You can specify a boolean for log checking (for making calls easier)
        /// </summary>
        /// <param name="message"></param>
        /// <param name="prefix"></param>
        /// <param name="shouldLog"></param>
        public static void Error(string message, bool shouldLog = true, string customPrefix = InternalLoggingPrefix)
        {
            if (shouldLog)
            {
                Log.Error($@"[{customPrefix}] {message}");
            }
        }

        /// <summary>
        /// Logs a string to the log. You can specify a boolean for log checking (for making calls easier)
        /// </summary>
        /// <param name="message"></param>
        /// <param name="prefix"></param>
        /// <param name="shouldLog"></param>
        public static void Error(Exception ex, bool shouldLog = true, string customPrefix = InternalLoggingPrefix)
        {
            if (shouldLog)
            {
                Log.Error($@"[{customPrefix}] {ex.FlattenException()}");
            }
        }

        /// <summary>
        /// Logs a string to the log. You can specify a boolean for log checking (for making calls easier)
        /// </summary>
        /// <param name="message"></param>
        /// <param name="prefix"></param>
        /// <param name="shouldLog"></param>
        public static void Fatal(string message, bool shouldLog = true, string customPrefix = InternalLoggingPrefix)
        {
            if (shouldLog)
            {
                Log.Fatal($@"[{customPrefix}] {message}");
            }
        }

        /// <summary>
        /// Calls Log.CloseAndFlush(). This is here just so everything routes through MLog.
        /// </summary>
        public static void CloseAndFlush()
        {
            Log.CloseAndFlush();
        }

        /// <summary>
        /// Writes a pre-message and a stacktrace to the log.
        /// </summary>
        /// <param name="exception"></param>
        /// <param name="preMessage"></param>
        /// <param name="fatal"></param>
        /// <param name="customPrefix"></param>
        public static void Exception(Exception exception, string preMessage, bool fatal = false, string customPrefix = InternalLoggingPrefix)
        {
            Log.Error($@"[{customPrefix}] {preMessage}");

            // Log exception
            while (exception != null)
            {
                var line1 = exception.GetType().Name + @": " + exception.Message;
                foreach (var line in line1.Split("\n")) // do not localize
                {
                    if (fatal)
                        Log.Fatal(customPrefix + line);
                    else
                        Log.Error(customPrefix + line);

                }

                if (exception.StackTrace != null)
                {
                    foreach (var line in exception.StackTrace.Split("\n")) // do not localize
                    {
                        if (fatal)
                            Log.Fatal(customPrefix + line);
                        else
                            Log.Error(customPrefix + line);
                    }
                }

                exception = exception.InnerException;
            }
        }

        /// <summary>
        /// Writes a debug log statement
        /// </summary>
        /// <param name="message"></param>
        /// <param name="prefix"></param>
        /// <param name="shouldLog"></param>
        /// <param name="customPrefix"></param>
        public static void Debug(string message, bool shouldLog = true, string customPrefix = InternalLoggingPrefix)
        {
            if (shouldLog)
            {
                Log.Debug($@"[{customPrefix}] {message}");
            }
        }
    }
}
