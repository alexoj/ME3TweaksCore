﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using CliWrap;
using CliWrap.EventStream;
using LegendaryExplorerCore.Gammtek.Extensions;
using LegendaryExplorerCore.Helpers;
using LegendaryExplorerCore.Packages;
using ME3TweaksCore.Misc;
using Serilog;

namespace ME3TweaksCore.Helpers.MEM
{

    [Flags]
    public enum LodSetting
    {
        Vanilla = 0,
        TwoK = 1,
        FourK = 2,
        SoftShadows = 4,
    }


    /// <summary>
    /// Utility class for interacting with MEM. Calls must be run on a background thread of
    /// </summary>
    public static class MEMIPCHandler
    {
        #region Static Property Changed
        public static event PropertyChangedEventHandler StaticPropertyChanged;

        /// <summary>
        /// Sets given property and notifies listeners of its change. IGNORES setting the property to same value.
        /// Should be called in property setters.
        /// </summary>
        /// <typeparam name="T">Type of given property.</typeparam>
        /// <param name="field">Backing field to update.</param>
        /// <param name="value">New value of property.</param>
        /// <param name="propertyName">Name of property.</param>
        /// <returns>True if success, false if backing field and new value aren't compatible.</returns>
        private static bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = "")
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            StaticPropertyChanged?.Invoke(null, new PropertyChangedEventArgs(propertyName));
            return true;
        }
        #endregion

        private static short _memNoGuiVersionOT = -1;
        public static short MassEffectModderNoGuiVersionOT
        {
            get => _memNoGuiVersionOT;
            set => SetProperty(ref _memNoGuiVersionOT, value);
        }

        private static short _memNoGuiVersionLE = -1;
        public static short MassEffectModderNoGuiVersionLE
        {
            get => _memNoGuiVersionLE;
            set => SetProperty(ref _memNoGuiVersionLE, value);
        }

        /// <summary>
        /// Returns the version number for MEM, or 0 if it couldn't be retreived
        /// </summary>
        /// <returns></returns>
        public static short GetMemVersion(bool classicMEM)
        {
            // If the current version doesn't support the --version --ipc, we just assume it is 0.
            MEMIPCHandler.RunMEMIPCUntilExit(classicMEM, @"--version --ipc", ipcCallback: (command, param) =>
            {
                if (command == @"VERSION")
                {
                    if (classicMEM)
                    {
                        MassEffectModderNoGuiVersionOT = short.Parse(param);
                    }
                    else
                    {
                        MassEffectModderNoGuiVersionLE = short.Parse(param);
                    }
                }
            });

            return classicMEM ? MassEffectModderNoGuiVersionOT : MassEffectModderNoGuiVersionLE;
        }

        public static void RunMEMIPCUntilExit(bool classicMEM,
            string arguments,
            Action<int> applicationStarted = null,
            Action<string, string> ipcCallback = null,
            Action<string> applicationStdErr = null,
            Action<int> applicationExited = null,
            Action<string> setMEMCrashLog = null,
            CancellationToken cancellationToken = default)
        {

            object lockObject = new object();
            void appStart(int processID)
            {
                applicationStarted?.Invoke(processID);
                // This might need to be waited on after method is called.
                Debug.WriteLine(@"Process launched. Process ID: " + processID);
            }
            void appExited(int code)
            {
                Debug.WriteLine($@"Process exited with code {code}");
                applicationExited?.Invoke(code);
                lock (lockObject)
                {
                    Monitor.Pulse(lockObject);
                }
            }

            StringBuilder crashLogBuilder = new StringBuilder();

            void memCrashLogOutput(string str)
            {
                crashLogBuilder.Append(str);
            }

            // Run MEM
            MEMIPCHandler.RunMEMIPC(classicMEM, arguments, appStart, ipcCallback, applicationStdErr, appExited, memCrashLogOutput,
                cancellationToken);

            // Wait until exit
            lock (lockObject)
            {
                Monitor.Wait(lockObject);
            }

            if (crashLogBuilder.Length > 0)
            {
                setMEMCrashLog?.Invoke(crashLogBuilder.ToString());
            }
        }

        private static async void RunMEMIPC(bool classicMEM, string arguments, Action<int> applicationStarted = null, Action<string, string> ipcCallback = null, Action<string> applicationStdErr = null, Action<int> applicationExited = null, Action<string> memCrashLine = null, CancellationToken cancellationToken = default)
        {
            bool exceptionOcurred = false;
            DateTime lastCacheoutput = DateTime.Now;
            void internalHandleIPC(string command, string parm)
            {
                switch (command)
                {
                    case @"CACHE_USAGE":
                        if (DateTime.Now > (lastCacheoutput.AddSeconds(10)))
                        {
                            Log.Information($@"[AICORE] MEM cache usage: {FileSize.FormatSize(long.Parse(parm))}");
                            lastCacheoutput = DateTime.Now;
                        }
                        break;
                    case @"EXCEPTION_OCCURRED": //An exception has occurred and MEM is going to crash
                        exceptionOcurred = true;
                        ipcCallback?.Invoke(command, parm);
                        break;
                    default:
                        ipcCallback?.Invoke(command, parm);
                        break;
                }
            }

            // No validation. Make sure exit code is checked in the calling process.
            var memPath = MCoreFilesystem.GetMEMNoGuiPath(classicMEM);

            var cmd = Cli.Wrap(memPath).WithArguments(arguments).WithValidation(CommandResultValidation.None);
            Debug.WriteLine($@"Launching process: {memPath} {arguments}");

            // GET MEM ENCODING
            FileVersionInfo mvi = FileVersionInfo.GetVersionInfo(memPath);
            Encoding encoding = mvi.FileMajorPart > 421 ? Encoding.Unicode : Encoding.UTF8; //? Is UTF8 the default for windows console

            await foreach (var cmdEvent in cmd.ListenAsync(encoding, cancellationToken))

            {
                switch (cmdEvent)
                {
                    case StartedCommandEvent started:
                        applicationStarted?.Invoke(started.ProcessId);
                        break;
                    case StandardOutputCommandEvent stdOut:
#if DEBUG
                        if (!stdOut.Text.StartsWith(@"[IPC]CACHE_USAGE"))
                        {
                            Debug.WriteLine(stdOut.Text);
                        }
#endif
                        if (stdOut.Text.StartsWith(@"[IPC]"))
                        {
                            var ipc = breakdownIPC(stdOut.Text);
                            internalHandleIPC(ipc.command, ipc.param);
                        }
                        else
                        {
                            if (exceptionOcurred)
                            {
                                Log.Fatal($@"[AICORE] {stdOut.Text}");
                                memCrashLine?.Invoke(stdOut.Text);
                            }
                        }
                        break;
                    case StandardErrorCommandEvent stdErr:
                        Debug.WriteLine(@"STDERR " + stdErr.Text);
                        if (exceptionOcurred)
                        {
                            Log.Fatal($@"[AICORE] {stdErr.Text}");
                        }
                        else
                        {
                            applicationStdErr?.Invoke(stdErr.Text);
                        }
                        break;
                    case ExitedCommandEvent exited:
                        applicationExited?.Invoke(exited.ExitCode);
                        break;
                }
            }
        }

        /// <summary>
        /// Converts MEM IPC output to command, param for handling. This method assumes string starts with [IPC] always.
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        private static (string command, string param) breakdownIPC(string str)
        {
            string command = str.Substring(5);
            int endOfCommand = command.IndexOf(' ');
            if (endOfCommand >= 0)
            {
                command = command.Substring(0, endOfCommand);
            }

            string param = str.Substring(endOfCommand + 5).Trim();
            return (command, param);
        }

        /// <summary>
        /// Sets the path MEM will use for the specified game
        /// </summary>
        /// <param name="targetGame"></param>
        /// <param name="targetPath"></param>
        /// <returns></returns>
        public static bool SetGamePath(bool classicMEM, MEGame targetGame, string targetPath)
        {
            int exitcode = 0;
            string args = $"--set-game-data-path --gameid {targetGame.ToMEMGameNum()} --path \"{targetPath}\""; //do not localize
            MEMIPCHandler.RunMEMIPCUntilExit(classicMEM, args, applicationExited: x => exitcode = x);
            if (exitcode != 0)
            {
                Log.Error($@"Non-zero MassEffectModderNoGui exit code setting game path: {exitcode}");
            }
            return exitcode == 0;
        }

        /// <summary>
        /// Sets the LODs as specified in the setting bitmask with MEM for the specified game
        /// </summary>
        /// <param name="game"></param>
        /// <param name="setting"></param>
        /// <returns></returns>
        public static bool SetLODs(MEGame game, LodSetting setting)
        {
            if (game.IsLEGame())
            {
                Log.Error(@"Cannot set LODs for LE games! This is a bug.");
                return false;
            }
            string args = $@"--apply-lods-gfx --gameid {game.ToGameNum()}";
            if (setting.HasFlag(LodSetting.SoftShadows))
            {
                args += @" --soft-shadows-mode --meuitm-mode";
            }

            if (setting.HasFlag(LodSetting.TwoK))
            {
                args += @" --limit-2k";
            }
            else if (setting.HasFlag(LodSetting.FourK))
            {
                // Nothing
            }
            else if (setting == LodSetting.Vanilla)
            {
                // Remove LODs
                args = $@"--remove-lods --gameid {game.ToGameNum()}";
            }

            int exitcode = -1;
            // We don't care about IPC on this
            MEMIPCHandler.RunMEMIPCUntilExit(true, args,
                null, null,
                x => Log.Error($@"StdError setting LODs: {x}"),
                x => exitcode = x); //Change to catch exit code of non zero.        
            if (exitcode != 0)
            {
                Log.Error($@"MassEffectModderNoGui had error setting LODs, exited with code {exitcode}");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Gets list of files in an archive
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public static List<string> GetFileListing(string file)
        {
            string args = $"--list-archive --input \"{file}\" --ipc"; //do not localize
            List<string> fileListing = new List<string>();

            int exitcode = -1;
            MEMIPCHandler.RunMEMIPCUntilExit(false, args,
                null,
                (command, param) =>
                {
                    if (command == @"FILENAME")
                    {
                        fileListing.Add(param);
                    }
                },
                x => Log.Error($@"StdError getting file listing for file {file}: {x}"),
                x => exitcode = x); //Change to catch exit code of non zero.        
            if (exitcode != 0)
            {
                Log.Error($@"MassEffectModderNoGui had error getting file listing of archive {file}, exit code {exitcode}");
            }
            return fileListing;
        }

        /// <summary>
        /// Fetches the list of LODs for the specified game
        /// </summary>
        /// <param name="game"></param>
        /// <returns></returns>
        public static Dictionary<string, string> GetLODs(MEGame game)
        {
            Dictionary<string, string> lods = new Dictionary<string, string>();
            var args = $@"--print-lods --gameid {game.ToMEMGameNum()} --ipc";
            int exitcode = -1;
            MEMIPCHandler.RunMEMIPCUntilExit(game.IsOTGame(), args, ipcCallback: (command, param) =>
            {
                switch (command)
                {
                    case @"LODLINE":
                        var lodSplit = param.Split(@"=");
                        try
                        {
                            lods[lodSplit[0]] = param.Substring(lodSplit[0].Length + 1);
                        }
                        catch (Exception e)
                        {
                            Log.Error($@"Error reading LOD line output from MEM: {param}, {e.Message}");
                        }

                        break;
                    default:
                        //Debug.WriteLine(@"oof?");
                        break;
                }
            },
                applicationExited: x => exitcode = x
            );
            if (exitcode != 0)
            {
                Log.Error($@"Error fetching LODs for {game}, exit code {exitcode}");
                return null; // Error getting LODs
            }

            return lods;
        }

        /// <summary>
        /// Used to pass data back to installer core. DO NOT CHANGE VALUES AS
        /// THEY ARE INDIRECTLY REFERENCED
        /// </summary>
        public enum GameDirPath
        {
            ME1GamePath,
            ME1ConfigPath,
            ME2GamePath,
            ME2ConfigPath,
            ME3GamePath,
            ME3ConfigPath,
        }

        /// <summary>
        /// Returns location of the game and config paths (on linux) as defined by MEM, or null if game can't be found.
        /// </summary>
        /// <param name="game"></param>
        /// <returns></returns>
        public static Dictionary<GameDirPath, string> GetGameLocations(bool originalTrilogy)
        {
            Dictionary<GameDirPath, string> result = new Dictionary<GameDirPath, string>();
            MEMIPCHandler.RunMEMIPCUntilExit(originalTrilogy, $@"--get-game-paths --ipc", ipcCallback: (command, param) =>
            {
                // THIS CODE ONLY WORKS ON OT
                // LE REPORTS DIFFERENTLY
                var spitIndex = param.IndexOf(' ');
                if (spitIndex < 0) return; // This is nothing
                var gameId = param.Substring(0, spitIndex);
                var path = Path.GetFullPath(param.Substring(spitIndex + 1, param.Length - (spitIndex + 1)));
                switch (command)
                {
                    case @"GAMEPATH":
                        {
                            var keyname = Enum.Parse<GameDirPath>($@"ME{gameId}GamePath");
                            if (param.Length > 1)
                            {
                                result[keyname] = path;
                            }
                            else
                            {
                                result[keyname] = null;
                            }
                            break;
                        }
                    case @"GAMECONFIGPATH":
                        {
                            var keyname = Enum.Parse<GameDirPath>($@"ME{gameId}ConfigPath");
                            if (param.Length > 1)
                            {
                                result[keyname] = path;
                            }
                            else
                            {
                                result[keyname] = null;
                            }
                            break;
                        }
                }
            });
            return result;
        }

#if !WINDOWS
                // Only works on Linux builds of MEM
                public static bool SetConfigPath(MEGame game, string itemValue)
                {
                    int exitcode = 0;
                    string args = $"--set-game-user-path --gameid {game.ToGameNum()} --path \"{itemValue}\""; //do not localize
                    MEMIPCHandler.RunMEMIPCUntilExit(args, applicationExited: x => exitcode = x);
                    if (exitcode != 0)
                    {
                        Log.Error($@"[AICORE] Non-zero MassEffectModderNoGui exit code setting game config path: {exitcode}");
                    }
                    return exitcode == 0;
                }
#endif
    }
}
