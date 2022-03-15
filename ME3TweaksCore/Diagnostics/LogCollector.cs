using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management;
using System.Reflection;
using System.Text;
using System.Threading;
using AuthenticodeExaminer;
using Flurl.Http;
using LegendaryExplorerCore.Compression;
using LegendaryExplorerCore.GameFilesystem;
using LegendaryExplorerCore.Gammtek.Extensions;
using LegendaryExplorerCore.Helpers;
using LegendaryExplorerCore.Misc;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Unreal;
using ME3TweaksCore.GameFilesystem;
using ME3TweaksCore.Helpers;
using ME3TweaksCore.Helpers.ME1;
using ME3TweaksCore.Helpers.MEM;
using ME3TweaksCore.Localization;
using ME3TweaksCore.Misc;
using ME3TweaksCore.NativeMods;
using ME3TweaksCore.Objects;
using ME3TweaksCore.Services;
using ME3TweaksCore.Services.BasegameFileIdentification;
using ME3TweaksCore.Services.ThirdPartyModIdentification;
using ME3TweaksCore.Targets;
using Microsoft.Win32;
using NickStrupat;
using Serilog;

namespace ME3TweaksCore.Diagnostics
{
    public class LogCollector
    {
        public static List<LogItem> GetLogsList()
        {
            var logs = Directory.GetFiles(MCoreFilesystem.GetLogDir(), "*.txt");
            return logs.Select(x => new LogItem(x)).ToList();
        }

        public static string CollectLogs(string logfile)
        {
            MLog.Information(@"Shutting down logger to allow application to pull log file.");
            Log.CloseAndFlush();
            try
            {
                string log = File.ReadAllText(logfile);
                CreateLogger();
                return log;
            }
            catch (Exception e)
            {
                CreateLogger();
                MLog.Error(@"Could not read log file! " + e.Message);
                return null;
            }
        }
        /// <summary>
        /// ILogger creation delegate that is invoked when reopening the logger after collecting logs.
        /// </summary>
        internal static Func<ILogger> CreateLogger { get; set; }


        // Following is an example CreateLogger call that can be used in consuming applications.
        //        internal static void CreateLogger()
        //        {
        //            Log.Logger = new LoggerConfiguration().WriteTo.SizeRollingFile(Path.Combine(App.LogDir, @"modmanagerlog.txt"),
        //                                    retainedFileDurationLimit: TimeSpan.FromDays(14),
        //                                    fileSizeLimitBytes: 1024 * 1024 * 10) // 10MB  
        //#if DEBUG
        //                .WriteTo.Debug()
        //#endif
        //                .CreateLogger();
        //        }

        /// <summary>
        /// Collects the latest log file and reopens the logger when complete (if specified)
        /// </summary>
        /// <param name="logdir"></param>
        /// <param name="restartLogger"></param>
        /// <returns></returns>
        public static string CollectLatestLog(string logdir, bool restartLogger)
        {
            MLog.Information(@"Closing application log to allow application to read log file");
            Log.CloseAndFlush();
            var logFile = new DirectoryInfo(logdir)
                                             .GetFiles(@"*.txt")
                                             .OrderByDescending(f => f.LastWriteTime)
                                             .FirstOrDefault();
            string logText = null;
            if (logFile != null && File.Exists(logFile.FullName))
            {
                try
                {
                    logText = File.ReadAllText(logFile.FullName);
                }
                catch (Exception e)
                {
                    MLog.Fatal($@"UNABLE TO READ LOG FILE {logFile.FullName}: {e.Message}");
                }
            }

            if (restartLogger)
            {
                CreateLogger?.Invoke();
            }
            return logText;
        }

        private enum Severity
        {
            INFO,
            WARN,
            ERROR,
            FATAL,
            GOOD,
            DIAGSECTION,
            BOLD,
            DLC,
            GAMEID,
            OFFICIALDLC,
            TPMI,
            SUB,
            BOLDBLUE,
            SUPERCEDANCE_FILE
        }


        //private static void runMassEffectModderNoGuiIPC(string operationName, string exe, string args, object lockObject, Action<string, string> exceptionOccuredCallback, Action<int?> setExitCodeCallback = null, Action<string, string> ipcCallback = null)
        //{
        //    MLog.Information($@"Running Mass Effect Modder No GUI w/ IPC: {exe} {args}");
        //    var memProcess = new ConsoleApp(exe, args);
        //    bool hasExceptionOccured = false;
        //    memProcess.ConsoleOutput += (o, args2) =>
        //    {
        //        string str = args2.Line;
        //        if (hasExceptionOccured)
        //        {
        //            Log.Fatal(@"MassEffectModderNoGui.exe: " + str);
        //        }
        //        if (str.StartsWith(@"[IPC]", StringComparison.Ordinal))
        //        {
        //            string command = str.Substring(5);
        //            int endOfCommand = command.IndexOf(' ');
        //            if (endOfCommand >= 0)
        //            {
        //                command = command.Substring(0, endOfCommand);
        //            }

        //            string param = str.Substring(endOfCommand + 5).Trim();
        //            if (command == @"EXCEPTION_OCCURRED")
        //            {
        //                hasExceptionOccured = true;
        //                exceptionOccuredCallback?.Invoke(operationName, param);
        //                return; //don't process this command further, nothing handles it.
        //            }

        //            ipcCallback?.Invoke(command, param);
        //        }
        //        //Debug.WriteLine(args2.Line);
        //    };
        //    memProcess.Exited += (a, b) =>
        //    {
        //        setExitCodeCallback?.Invoke(memProcess.ExitCode);
        //        lock (lockObject)
        //        {
        //            Monitor.Pulse(lockObject);
        //        }
        //    };
        //    memProcess.Run();
        //}

        public static string PerformDiagnostic(LogUploadPackage package)
        {
            MLog.Information($@"Collecting diagnostics for target {package.DiagnosticTarget.TargetPath}");
            package.UpdateStatusCallback?.Invoke(LC.GetString(LC.string_preparingToCollectDiagnosticInfo));
            package.UpdateTaskbarProgressStateCallback?.Invoke(MTaskbarState.Indeterminate);

            #region MEM No Gui Fetch
            object memEnsuredSignaler = new object();
            // It is here we say a little prayer
            // to keep the bugs away from this monsterous code
            //    /_/\/\
            //    \_\  /
            //    /_/  \
            //    \_\/\ \
            //      \_\/

            bool hasMEM = false;
            #region MEM Fetch Callbacks
            //void readyToLaunch(string exe)
            //{
            //    Thread.Sleep(100); //try to stop deadlock
            //    hasMEM = true;
            //    mempath = exe;
            //    lock (memEnsuredSignaler)
            //    {
            //        Monitor.Pulse(memEnsuredSignaler);
            //    }
            //};

            void failedToExtractMEM(Exception e)
            {
                Thread.Sleep(100); //try to stop deadlock
                hasMEM = false;
            }
            void currentTaskCallback(string s) => package.UpdateStatusCallback?.Invoke(s);
            void setPercentDone(long downloaded, long total) => package.UpdateStatusCallback?.Invoke(LC.GetString(LC.string_interp_preparingMEMNoGUIX, MUtilities.GetPercent(downloaded, total)));

            #endregion
            // Ensure MEM NOGUI
            if (package.DiagnosticTarget != null)
            {
                hasMEM = MEMNoGuiUpdater.UpdateMEM(package.DiagnosticTarget.Game.IsOTGame(), false, setPercentDone, failedToExtractMEM, currentTaskCallback);
            }

            //wait for tool fetch
            //if (!hasMEM)
            //{
            //    lock (memEnsuredSignaler)
            //    {
            //        Monitor.Wait(memEnsuredSignaler, new TimeSpan(0, 0, 25));
            //    }
            //}
            #endregion
            MLog.Information(@"Completed MEM fetch task");

            #region Diagnostic setup and diag header
            package.UpdateStatusCallback?.Invoke(LC.GetString(LC.string_collectingGameInformation));
            var diagStringBuilder = new StringBuilder();

            void addDiagLines(IEnumerable<string> strings, Severity sev = Severity.INFO)
            {
                foreach (var s in strings)
                {
                    addDiagLine(s, sev);
                }
            }

            void addDiagLine(string message = "", Severity sev = Severity.INFO)
            {
                if (diagStringBuilder == null)
                {
                    diagStringBuilder = new StringBuilder();
                }

                switch (sev)
                {
                    case Severity.INFO:
                        diagStringBuilder.Append(message);
                        break;
                    case Severity.WARN:
                        diagStringBuilder.Append($@"[WARN]{message}");
                        break;
                    case Severity.ERROR:
                        diagStringBuilder.Append($@"[ERROR]{message}");
                        break;
                    case Severity.FATAL:
                        diagStringBuilder.Append($@"[FATAL]{message}");
                        break;
                    case Severity.DIAGSECTION:
                        diagStringBuilder.Append($@"[DIAGSECTION]{message}");
                        break;
                    case Severity.GOOD:
                        diagStringBuilder.Append($@"[GREEN]{message}");
                        break;
                    case Severity.BOLD:
                        diagStringBuilder.Append($@"[BOLD]{message}");
                        break;
                    case Severity.BOLDBLUE:
                        diagStringBuilder.Append($@"[BOLDBLUE]{message}");
                        break;
                    case Severity.DLC:
                        diagStringBuilder.Append($@"[DLC]{message}");
                        break;
                    case Severity.OFFICIALDLC:
                        diagStringBuilder.Append($@"[OFFICIALDLC]{message}");
                        break;
                    case Severity.GAMEID:
                        diagStringBuilder.Append($@"[GAMEID]{message}");
                        break;
                    case Severity.TPMI:
                        diagStringBuilder.Append($@"[TPMI]{message}");
                        break;
                    case Severity.SUB:
                        diagStringBuilder.Append($@"[SUB]{message}");
                        break;
                    case Severity.SUPERCEDANCE_FILE:
                        diagStringBuilder.Append($@"[SSF]{message}");
                        break;
                    default:
                        Debugger.Break();
                        break;
                }
                diagStringBuilder.Append("\n"); //do not localize
            }


            string gamePath = package.DiagnosticTarget.TargetPath;
            var gameID = package.DiagnosticTarget.Game.ToMEMGameNum().ToString();
            MLog.Information(@"Beginning to build diagnostic output");

            addDiagLine(package.DiagnosticTarget.Game.ToGameNum().ToString(), Severity.GAMEID);
            addDiagLine($@"ME3TweaksCore {Assembly.GetExecutingAssembly().GetName().Version} Game Diagnostic");
            addDiagLine($@"Diagnostic for {package.DiagnosticTarget.Game.ToGameName()}");
            addDiagLine($@"Diagnostic generated on {DateTime.Now.ToShortDateString()}");
            #endregion

            #region MEM Setup
            //vars
            string args = null;
            int exitcode = -1;

            //paths
            string oldMemGamePath = null;
            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"MassEffectModder");
            string _iniPath = Path.Combine(path, package.DiagnosticTarget.Game.IsLEGame() ? @"MassEffectModderLE.ini" : @"MassEffectModder.ini");
            if (hasMEM)
            {
                // Set INI path to target

                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                if (!File.Exists(_iniPath))
                {
                    File.Create(_iniPath).Close();
                }

                // TODO: DUPLICATING INI NEEDS LATEST VERSION.
                DuplicatingIni ini = DuplicatingIni.LoadIni(_iniPath);

                if (package.DiagnosticTarget.Game.IsLEGame())
                {
                    oldMemGamePath = ini[@"GameDataPath"][@"MELE"].Value;
                    var rootPath = Directory.GetParent(gamePath);
                    if (rootPath != null)
                        rootPath = Directory.GetParent(rootPath.FullName);
                    if (rootPath != null)
                    {
                        ini[@"GameDataPath"][@"MELE"].Value = rootPath.FullName;
                    }
                    else
                    {
                        MLog.Error($@"Invalid game directory: {gamePath} is not part of an overall LE install");
                        addDiagLine($@"MEM diagnostics skipped: Game directory is not part of an overall LE install", Severity.ERROR);
                        hasMEM = false;
                    }
                }
                else
                {
                    oldMemGamePath = ini[@"GameDataPath"][package.DiagnosticTarget.Game.ToString()]?.Value;
                    ini[@"GameDataPath"][package.DiagnosticTarget.Game.ToString()].Value = gamePath;
                }

                if (hasMEM)
                {
                    File.WriteAllText(_iniPath, ini.ToString());
                    var versInfo = FileVersionInfo.GetVersionInfo(MCoreFilesystem.GetMEMNoGuiPath(package.DiagnosticTarget.Game.IsOTGame()));
                    int fileVersion = versInfo.FileMajorPart;
                    addDiagLine($@"Diagnostic MassEffectModderNoGui version: {fileVersion}");
                }
            }
            else
            {
                addDiagLine(@"Mass Effect Modder No Gui was not available for use when this diagnostic was generated.", Severity.ERROR);
            }
            #endregion

            addDiagLine($@"System culture: {CultureInfo.InstalledUICulture.Name}");
            try
            {
                #region Game Information
                MLog.Information(@"Collecting basic game information");

                package.UpdateStatusCallback?.Invoke(LC.GetString(LC.string_collectingGameInformation));
                addDiagLine(@"Basic game information", Severity.DIAGSECTION);
                addDiagLine($@"Game is installed at {gamePath}");

                MLog.Information(@"Reloading target for most up to date information");
                package.DiagnosticTarget.ReloadGameTarget(false); //reload vars
                TextureModInstallationInfo avi = package.DiagnosticTarget.GetInstalledALOTInfo();

                string exePath = M3Directories.GetExecutablePath(package.DiagnosticTarget);
                if (File.Exists(exePath))
                {
                    MLog.Information(@"Getting game version");
                    var versInfo = FileVersionInfo.GetVersionInfo(exePath);
                    addDiagLine($@"Version: {versInfo.FileMajorPart}.{versInfo.FileMinorPart}.{versInfo.FileBuildPart}.{versInfo.FilePrivatePart}");

                    //Disk type
                    string pathroot = Path.GetPathRoot(gamePath);
                    pathroot = pathroot.Substring(0, 1);
                    if (pathroot == @"\")
                    {
                        addDiagLine(@"Installation appears to be on a network drive (first character in path is \)", Severity.WARN);
                    }
                    else
                    {
                        if (MUtilities.IsWindows10OrNewer())
                        {
                            int backingType = GetPartitionDiskBackingType(pathroot);
                            string type = @"Unknown type";
                            switch (backingType)
                            {
                                case 3:
                                    type = @"Hard disk drive";
                                    break;
                                case 4:
                                    type = @"Solid state drive";
                                    break;
                                default:
                                    type += @": " + backingType;
                                    break;
                            }

                            addDiagLine(@"Installed on disk type: " + type);
                        }
                    }

                    if (package.DiagnosticTarget.Supported)
                    {
                        addDiagLine($@"Game source: {package.DiagnosticTarget.GameSource}", Severity.GOOD);
                    }
                    else
                    {
                        addDiagLine($@"Game source: Unknown/Unsupported - {package.DiagnosticTarget.ExecutableHash}", Severity.FATAL);
                    }

                    if (package.DiagnosticTarget.Game == MEGame.ME1)
                    {
                        MLog.Information(@"Getting additional ME1 executable information");
                        var exeInfo = ME1ExecutableInfo.GetExecutableInfo(M3Directories.GetExecutablePath(package.DiagnosticTarget), false);
                        if (avi != null)
                        {
                            addDiagLine($@"Large Address Aware: {exeInfo.HasLAAApplied}", exeInfo.HasLAAApplied ? Severity.GOOD : Severity.FATAL);
                            addDiagLine($@"No-admin patched: {exeInfo.HasLAAApplied}", exeInfo.HasProductNameChanged ? Severity.GOOD : Severity.WARN);
                            addDiagLine($@"enableLocalPhysXCore patched: {exeInfo.HasPhysXCoreChanged}", exeInfo.HasLAAApplied ? Severity.GOOD : Severity.WARN);
                        }
                        else
                        {
                            addDiagLine($@"Large Address Aware: {exeInfo.HasLAAApplied}");
                            addDiagLine($@"No-admin patched: {exeInfo.HasLAAApplied}");
                            addDiagLine($@"enableLocalPhysXCore patched: {exeInfo.HasLAAApplied}");
                        }
                    }

                    //Executable signatures
                    MLog.Information(@"Checking executable signature");

                    var info = new FileInspector(exePath);
                    var certOK = info.Validate();
                    if (certOK == SignatureCheckResult.NoSignature)
                    {
                        addDiagLine(@"This executable is not signed", Severity.ERROR);
                    }
                    else
                    {
                        if (certOK == SignatureCheckResult.BadDigest)
                        {
                            if (package.DiagnosticTarget.Game == MEGame.ME1 && versInfo.ProductName == @"Mass_Effect")
                            {
                                //Check if this Mass_Effect
                                addDiagLine(@"Signature check for this executable was skipped as MEM modified this exe");
                            }
                            else
                            {
                                addDiagLine(@"The signature for this executable is not valid. The executable has been modified", Severity.ERROR);
                                diagPrintSignatures(info, addDiagLine);
                            }
                        }
                        else
                        {
                            addDiagLine(@"Signature check for this executable: " + certOK.ToString());
                            diagPrintSignatures(info, addDiagLine);
                        }
                    }

                    //BINK
                    MLog.Information(@"Checking if Bink ASI loader is installed");

                    if (package.DiagnosticTarget.CheckIfBinkw32ASIIsInstalled())
                    {
                        addDiagLine(@"binkw32 ASI bypass is installed");
                    }
                    else
                    {
                        addDiagLine(@"binkw32 ASI bypass is not installed. DLC mods, ASI mods, and modified DLC will not load", Severity.WARN);
                    }

                    if (package.DiagnosticTarget.Game == MEGame.ME1)
                    {
                        // Check for patched PhysX
                        if (ME1PhysXTools.IsPhysXLoaderPatchedLocalOnly(package.DiagnosticTarget))
                        {
                            addDiagLine(@"PhysXLoader.dll is patched to force local PhysXCore.dll", Severity.GOOD);
                        }
                        else if (certOK == SignatureCheckResult.BadDigest)
                        {
                            addDiagLine(@"PhysXLoader.dll is not patched to force local PhysXCore.dll. Game may not boot", Severity.WARN);
                        }
                        else if (certOK == SignatureCheckResult.Valid)
                        {
                            addDiagLine(@"PhysXLoader.dll is not patched, but executable is still signed", Severity.GOOD);
                        }
                        else
                        {
                            addDiagLine(@"PhysXLoader.dll status could not be checked", Severity.WARN);
                        }
                    }

                    package.DiagnosticTarget.PopulateExtras();
                    if (package.DiagnosticTarget.ExtraFiles.Any())
                    {
                        addDiagLine(@"Additional dll files found in game executable directory:", Severity.WARN);
                        foreach (var extra in package.DiagnosticTarget.ExtraFiles)
                        {
                            addDiagLine(@" > " + extra.DisplayName);
                        }
                    }
                }

                #endregion

                #region System Information
                MLog.Information(@"Collecting system information");

                package.UpdateStatusCallback?.Invoke(LC.GetString(LC.string_collectingSystemInformation));

                addDiagLine(@"System information", Severity.DIAGSECTION);
                OperatingSystem os = Environment.OSVersion;
                Version osBuildVersion = os.Version;

                //Windows 10 only
                string releaseId = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion", @"ReleaseId", "").ToString();
                string productName = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion", @"ProductName", "").ToString();
                string verLine = @"Running " + productName;
                if (osBuildVersion.Major == 10)
                {
                    verLine += @" " + releaseId;
                }

                if (os.Version < ME3TweaksCoreLib.MIN_SUPPORTED_OS)
                {
                    addDiagLine(@"This operating system is not supported", Severity.FATAL);
                    addDiagLine(@"Upgrade to a supported operating system if you want support", Severity.FATAL);
                }

                addDiagLine(verLine, os.Version < ME3TweaksCoreLib.MIN_SUPPORTED_OS ? Severity.ERROR : Severity.INFO);
                addDiagLine(@"Version " + osBuildVersion, os.Version < ME3TweaksCoreLib.MIN_SUPPORTED_OS ? Severity.ERROR : Severity.INFO);

                addDiagLine();
                MLog.Information(@"Collecting memory information");

                addDiagLine(@"System Memory", Severity.BOLD);
                var computerInfo = new ComputerInfo();
                long ramInBytes = (long)computerInfo.TotalPhysicalMemory;
                addDiagLine($@"Total memory available: {FileSize.FormatSize(ramInBytes)}");
                addDiagLine(@"Processors", Severity.BOLD);
                MLog.Information(@"Collecting processor information");

                addDiagLine(GetProcessorInformationForDiag());
                if (ramInBytes == 0)
                {
                    addDiagLine(@"Unable to get the read amount of physically installed ram. This may be a sign of impending hardware failure in the SMBIOS", Severity.WARN);
                }

                MLog.Information(@"Collecting video card information");

                /* OLD CODE HERE
                                ManagementObjectSearcher objvide = new ManagementObjectSearcher(@"select * from Win32_VideoController");
                int vidCardIndex = 1;
                foreach (ManagementObject obj in objvide.Get())
                {
                    addDiagLine();
                    addDiagLine(@"Video Card " + vidCardIndex, Severity.BOLD);
                    addDiagLine(@"Name: " + obj[@"Name"]);

                    //Get Memory
                    string vidKey = @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\";
                    vidKey += (vidCardIndex - 1).ToString().PadLeft(4, '0');
                    object returnvalue = null;
                    try
                    {
                        returnvalue = Registry.GetValue(vidKey, @"HardwareInformation.qwMemorySize", 0L);
                    }
                    catch (Exception ex)
                    {
                        addDiagLine($@"Unable to read memory size from registry. Reading from WMI instead ({ex.GetType()})", Severity.WARN);
                    }

                    string displayVal;
                    if (returnvalue is long size && size != 0)
                    {
                        displayVal = FileSize.FormatSize(size);
                    }
                    else
                    {
                        try
                        {
                            UInt32 wmiValue = (UInt32)obj[@"AdapterRam"];
                            var numBytes = (long)wmiValue;

                            // TODO: UPDATE THIS FOR FILESIZE. NEEDS TESTING
                            displayVal = FileSize.FormatSize(numBytes);
                            if (numBytes == uint.MaxValue)
                            {
                                displayVal += @" (possibly more, variable is 32-bit unsigned)";
                            }
                        }
                        catch (Exception)
                        {
                            displayVal = @"Unable to read value from registry/WMI";
                        }
                    }

                    addDiagLine(@"Memory: " + displayVal);
                    addDiagLine(@"DriverVersion: " + obj[@"DriverVersion"]);
                    vidCardIndex++;
                }*/

                // Enumerate the Display Adapters registry key
                int vidCardIndex = 1;
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}"))
                {
                    if (key != null)
                    {
                        var subNames = key.GetSubKeyNames();
                        foreach (string adapterRegistryIndex in subNames)
                        {
                            if (!int.TryParse(adapterRegistryIndex, out _)) continue; // Only go into numerical ones
                            try
                            {
                                var videoKey = key.OpenSubKey(adapterRegistryIndex);

                                // Get memory. If memory is not populated then this is not active right now (I think)
                                long vidCardSizeInBytes = (long)videoKey.GetValue(@"HardwareInformation.qwMemorySize", 0L);
                                if (vidCardSizeInBytes == 0) continue; // Not defined

                                string vidCardName = (string)videoKey.GetValue(@"HardwareInformation.AdapterString", @"Unable to get adapter name");
                                string vidDriverVersion = (string)videoKey.GetValue(@"DriverVersion", @"Unable to get driver version");
                                string vidDriverDate = (string)videoKey.GetValue(@"DriverDate", @"Unable to get driver date");

                                addDiagLine();
                                addDiagLine($@"Video Card {(vidCardIndex++)}", Severity.BOLD);
                                addDiagLine($@"Name: {vidCardName}");
                                addDiagLine($@"Memory: {FileSize.FormatSize(vidCardSizeInBytes)}");
                                addDiagLine($@"Driver Version: {vidDriverVersion}");
                                addDiagLine($@"Driver Date: {vidDriverDate}");
                            }
                            catch (Exception ex)
                            {
                                addDiagLine($@"Error getting video card information: {ex.Message}", Severity.WARN);
                            }
                        }
                    }
                }

                // Antivirus
                var avs = MUtilities.GetListOfInstalledAV();
                addDiagLine(@"Antivirus products", Severity.BOLD);
                addDiagLine(@"The following antivirus products were detected:");
                foreach (var av in avs)
                {
                    addDiagLine($@"- {av}");
                }
                #endregion

                #region Texture mod information

                MLog.Information(@"Getting texture mod installation info");
                package.UpdateStatusCallback?.Invoke(LC.GetString(LC.string_gettingTextureInfo));
                addDiagLine(@"Current texture mod information", Severity.DIAGSECTION);

                var textureHistory = package.DiagnosticTarget.GetTextureModInstallationHistory();
                if (!textureHistory.Any())
                {
                    addDiagLine(
                        @"The texture mod installation marker was not detected. No texture mods appear to be installed");
                }
                else
                {
                    var latestInstall = textureHistory[0];
                    if (latestInstall.ALOTVER > 0 || latestInstall.MEUITMVER > 0)
                    {
                        addDiagLine($@"ALOT version: {latestInstall.ALOTVER}.{latestInstall.ALOTUPDATEVER}.{latestInstall.ALOTHOTFIXVER}");
                        if (latestInstall.MEUITMVER != 0)
                        {
                            var meuitmName = package.DiagnosticTarget.Game == MEGame.ME1 ? @"MEUITM" : $@"MEUITM{package.DiagnosticTarget.Game.ToGameNum()}";
                            addDiagLine($@"{meuitmName} version: {latestInstall.MEUITMVER}");
                        }
                    }
                    else
                    {
                        addDiagLine(@"This installation has been texture modded, but ALOT and/or MEUITM has not been installed");
                    }

                    if (latestInstall.MarkerExtendedVersion >= TextureModInstallationInfo.FIRST_EXTENDED_MARKER_VERSION && !string.IsNullOrWhiteSpace(latestInstall.InstallerVersionFullName))
                    {
                        addDiagLine($@"Latest installation was from performed by {latestInstall.InstallerVersionFullName}");
                    }
                    else if (latestInstall.ALOT_INSTALLER_VERSION_USED > 0)
                    {
                        addDiagLine($@"Latest installation was from installer v{latestInstall.ALOT_INSTALLER_VERSION_USED}");
                    }

                    addDiagLine($@"Latest installation used MEM v{latestInstall.MEM_VERSION_USED}");

                    addDiagLine(@"Texture mod installation history", Severity.DIAGSECTION);
                    addDiagLine(@"The history of texture mods installed into this game is as follows (from latest install to first install):");

                    addDiagLine(@"Click to view list", Severity.SUB);
                    bool isFirst = true;
                    foreach (var tmii in textureHistory)
                    {
                        if (isFirst)
                            isFirst = false;
                        else
                            addDiagLine();

                        if (tmii.MarkerExtendedVersion >= TextureModInstallationInfo.FIRST_EXTENDED_MARKER_VERSION)
                        {
                            addDiagLine($@"Texture install on {tmii.InstallationTimestamp:yyyy MMMM dd h:mm:ss tt zz}", Severity.BOLDBLUE);
                        }
                        else
                        {
                            addDiagLine(@"Texture install", Severity.BOLDBLUE);
                        }

                        addDiagLine($@"Marker version {tmii.MarkerExtendedVersion}");
                        addDiagLine(tmii.ToString());
                        if (tmii.MarkerExtendedVersion >= 3 && !string.IsNullOrWhiteSpace(tmii.InstallerVersionFullName))
                        {
                            addDiagLine($@"Installation was from performed by {tmii.InstallerVersionFullName}");
                        }
                        else if (tmii.ALOT_INSTALLER_VERSION_USED > 0)
                        {
                            addDiagLine($@"Installation was performed by installer v{tmii.ALOT_INSTALLER_VERSION_USED}");
                        }

                        addDiagLine($@"Installed used MEM v{tmii.MEM_VERSION_USED}");

                        if (tmii.InstalledTextureMods.Any())
                        {
                            addDiagLine(@"Files installed in session:");
                            foreach (var fi in tmii.InstalledTextureMods)
                            {
                                var modStr = @" - ";
                                if (fi.ModType == InstalledTextureMod.InstalledTextureModType.USERFILE)
                                {
                                    modStr += @"[USERFILE] ";
                                }

                                modStr += fi.ModName;
                                if (!string.IsNullOrWhiteSpace(fi.AuthorName))
                                {
                                    modStr += $@" by {fi.AuthorName}";
                                }

                                addDiagLine(modStr, fi.ModType == InstalledTextureMod.InstalledTextureModType.USERFILE ? Severity.WARN : Severity.GOOD);
                                if (fi.ChosenOptions.Any())
                                {
                                    addDiagLine(@"   Chosen options for install:");
                                    foreach (var c in fi.ChosenOptions)
                                    {
                                        addDiagLine($@"      {c}");
                                    }
                                }
                            }
                        }
                    }

                    addDiagLine(@"[/SUB]");
                }

                #endregion

                #region Basegame file changes
                MLog.Information(@"Collecting basegame file changes information");

                addDiagLine(@"Basegame changes", Severity.DIAGSECTION);

                package.UpdateStatusCallback?.Invoke(@"Collecting basegame file modifications");
                List<string> modifiedFiles = new List<string>();

                void failedCallback(string file)
                {
                    modifiedFiles.Add(file);
                }

                var isVanilla = VanillaDatabaseService.ValidateTargetAgainstVanilla(package.DiagnosticTarget, failedCallback, false);
                if (isVanilla)
                {
                    addDiagLine(@"No modified basegame files were found.");
                }
                else
                {
                    if (!package.DiagnosticTarget.TextureModded)
                    {
                        var modifiedBGFiles = new List<string>();
                        var cookedPath = M3Directories.GetCookedPath(package.DiagnosticTarget);
                        var markerPath = M3Directories.GetTextureMarkerPath(package.DiagnosticTarget);
                        foreach (var mf in modifiedFiles)
                        {
                            if (mf.StartsWith(cookedPath, StringComparison.InvariantCultureIgnoreCase))
                            {
                                if (mf.Equals(markerPath, StringComparison.InvariantCultureIgnoreCase)) continue; //don't report this file
                                var info = BasegameFileIdentificationService.GetBasegameFileSource(package.DiagnosticTarget, mf);
                                if (info != null)
                                {
                                    modifiedBGFiles.Add($@" - {mf.Substring(cookedPath.Length + 1)} - {info.source}");
                                }
                                else
                                {
                                    modifiedBGFiles.Add($@" - {mf.Substring(cookedPath.Length + 1)}");
                                }
                            }
                        }

                        if (modifiedBGFiles.Any())
                        {
                            addDiagLine(@"The following basegame files have been modified:");
                            foreach (var mbgf in modifiedBGFiles)
                            {
                                addDiagLine(mbgf);
                            }
                        }
                        else
                        {
                            addDiagLine(@"No modified basegame files were found");
                        }

                    }
                    else
                    {
                        //Check MEMI markers?
                        addDiagLine(@"Basegame changes check skipped as this installation has been texture modded");
                    }
                }

                #endregion

                #region Blacklisted mods check
                MLog.Information(@"Checking for mods that are known to cause problems in the scene");

                void memExceptionOccured(string operation)
                {
                    addDiagLine($@"An exception occurred performing an operation: {operation}", Severity.ERROR);
                    addDiagLine(@"Check the Mod Manager application log for more information.", Severity.ERROR);
                    addDiagLine(@"Report this on the ME3Tweaks Discord for further assistance.", Severity.ERROR);
                }

                if (hasMEM)
                {
                    package.UpdateStatusCallback?.Invoke(@"Checking for blacklisted mods");
                    args = $@"--detect-bad-mods --gameid {gameID} --ipc";
                    var blacklistedMods = new List<string>();
                    MEMIPCHandler.RunMEMIPCUntilExit(package.DiagnosticTarget.Game.IsOTGame(), args, setMEMCrashLog: memExceptionOccured, ipcCallback: (string command, string param) =>
                     {
                         switch (command)
                         {
                             case @"ERROR":
                                 blacklistedMods.Add(param);
                                 break;
                             default:
                                 Debug.WriteLine(@"oof?");
                                 break;
                         }
                     }, applicationExited: x => exitcode = x);

                    if (exitcode != 0)
                    {
                        addDiagLine(
                            $@"MassEffectModderNoGuiexited exited incompatible mod detection check with code {exitcode}",
                            Severity.ERROR);
                    }

                    if (blacklistedMods.Any())
                    {
                        addDiagLine(@"The following blacklisted mods were found:", Severity.ERROR);
                        foreach (var str in blacklistedMods)
                        {
                            addDiagLine(@" - " + str);
                        }

                        addDiagLine(@"These mods have been blacklisted by modding tools because of known issues they cause. Do not use these mods", Severity.ERROR);
                    }
                    else
                    {
                        addDiagLine(@"No blacklisted mods were found installed");
                    }
                }
                else
                {
                    addDiagLine(@"MEM not available, skipped blacklisted mods check", Severity.WARN);

                }

                #endregion

                #region Installed DLCs
                MLog.Information(@"Collecting installed DLC");

                //Get DLCs
                package.UpdateStatusCallback?.Invoke(LC.GetString(LC.string_collectingDLCInformation));

                var installedDLCs = package.DiagnosticTarget.GetMetaMappedInstalledDLC();

                addDiagLine(@"Installed DLC", Severity.DIAGSECTION);
                addDiagLine(@"The following DLC is installed:");

                var officialDLC = MEDirectories.OfficialDLC(package.DiagnosticTarget.Game);
                foreach (var dlc in installedDLCs)
                {
                    string dlctext = dlc.Key;
                    if (!officialDLC.Contains(dlc.Key, StringComparer.InvariantCultureIgnoreCase))
                    {
                        dlctext += @";;";
                        if (dlc.Value != null)
                        {
                            if (int.TryParse(dlc.Value.InstalledBy, out var _))
                            {
                                dlctext += @"Installed by Mod Manager Build " + dlc.Value.InstalledBy;
                            }
                            else
                            {
                                dlctext += @"Installed by " + dlc.Value.InstalledBy;
                            }
                            if (!string.IsNullOrWhiteSpace(dlc.Value.Version))
                            {
                                dlctext += @";;" + dlc.Value.Version;
                            }
                        }
                        else
                        {
                            dlctext += @"Not installed by managed installer";
                        }
                    }

                    var isOfficialDLC = officialDLC.Contains(dlc.Key, StringComparer.InvariantCultureIgnoreCase);
                    addDiagLine(dlctext, isOfficialDLC ? Severity.OFFICIALDLC : Severity.DLC);

                    if (!isOfficialDLC)
                    {
                        if (dlc.Value != null && dlc.Value.OptionsSelectedAtInstallTime.Any())
                        {
                            // Print options
                            //addDiagLine(@"   > The following options were chosen at install time:");
                            foreach (var o in dlc.Value.OptionsSelectedAtInstallTime)
                            {
                                addDiagLine(($@"   > {o}"));
                            }
                        }
                    }
                }

                if (installedDLCs.Any())
                {
                    SeeIfIncompatibleDLCIsInstalled(package.DiagnosticTarget, addDiagLine);
                }

                // 03/13/2022: Supercedance list now lists all DLC files even if they don't supercede anything.
                MLog.Information(@"Collecting supersedance list");
                var supercedanceList = M3Directories.GetFileSupercedances(package.DiagnosticTarget).ToList();
                if (supercedanceList.Any())
                {
                    addDiagLine();
                    addDiagLine(@"DLC mod files", Severity.BOLD);
                    addDiagLine(@"The following DLC mod files are installed, as well as their supercedances (if any). This may mean the mods are incompatible, or that these files are compatibility patches. This information is for developer use only - DO NOT MODIFY YOUR GAME DIRECTORY MANUALLY.");

                    bool isFirst = true;
                    addDiagLine(@"Click to view list", Severity.SUB);
                    foreach (var sl in supercedanceList.OrderBy(x => x.Key))
                    {
                        if (isFirst)
                            isFirst = false;
                        else
                            addDiagLine();

                        addDiagLine(sl.Key, Severity.SUPERCEDANCE_FILE);
                        foreach (var dlc in sl.Value)
                        {
                            addDiagLine(dlc, Severity.TPMI);
                        }
                    }
                    addDiagLine(@"[/SUB]");
                }
                #endregion

                #region Get list of TFCs

                if (package.DiagnosticTarget.Game > MEGame.ME1)
                {
                    MLog.Information(@"Getting list of TFCs");
                    package.UpdateStatusCallback?.Invoke(LC.GetString(LC.string_collectingTFCFileInformation));

                    addDiagLine(@"Texture File Cache (TFC) files", Severity.DIAGSECTION);
                    addDiagLine(@"The following TFC files are present in the game directory.");
                    var bgPath = M3Directories.GetBioGamePath(package.DiagnosticTarget);
                    string[] tfcFiles = Directory.GetFiles(bgPath, @"*.tfc", SearchOption.AllDirectories);
                    if (tfcFiles.Any())
                    {
                        foreach (string tfc in tfcFiles)
                        {
                            FileInfo fi = new FileInfo(tfc);
                            long tfcSize = fi.Length;
                            string tfcPath = tfc.Substring(bgPath.Length + 1);
                            addDiagLine($@" - {tfcPath}, {FileSize.FormatSize(tfcSize)}"); //do not localize
                        }
                    }
                    else
                    {
                        addDiagLine(@"No TFC files were found - is this installation broken?", Severity.ERROR);
                    }
                }

                #endregion

                if (hasMEM)
                {
                    #region Files added or removed after texture install
                    MLog.Information(@"Finding files that have been added/replaced/removed after textures were installed");

                    args = $@"--check-game-data-mismatch --gameid {gameID} --ipc";
                    if (package.DiagnosticTarget.TextureModded)
                    {
                        // Is this correct on linux?
                        MLog.Information(@"Checking texture map is in sync with game state");

                        bool textureMapFileExists = File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + $@"\MassEffectModder\me{gameID}map.bin");
                        addDiagLine(@"Files added or removed after texture mods were installed", Severity.DIAGSECTION);

                        if (textureMapFileExists)
                        {
                            // check for replaced files (file size changes)
                            package.UpdateStatusCallback?.Invoke(LC.GetString(LC.string_checkingTextureMapGameConsistency));
                            List<string> removedFiles = new List<string>();
                            List<string> addedFiles = new List<string>();
                            List<string> replacedFiles = new List<string>();
                            MEMIPCHandler.RunMEMIPCUntilExit(package.DiagnosticTarget.Game.IsOTGame(), args, setMEMCrashLog: memExceptionOccured, ipcCallback: (string command, string param) =>
                            {
                                switch (command)
                                {
                                    case @"ERROR_REMOVED_FILE":
                                        //.Add($" - File removed after textures were installed: {param}");
                                        removedFiles.Add(param);
                                        break;
                                    case @"ERROR_ADDED_FILE":
                                        //addedFiles.Add($"File was added after textures were installed" + param + " " + File.GetCreationTimeUtc(Path.Combine(gamePath, param));
                                        addedFiles.Add(param);
                                        break;
                                    case @"ERROR_VANILLA_MOD_FILE":
                                        if (!addedFiles.Contains(param))
                                        {
                                            replacedFiles.Add(param);
                                        }
                                        break;
                                    default:
                                        Debug.WriteLine(@"oof?");
                                        break;
                                }
                            },
                            applicationExited: i => exitcode = i);
                            if (exitcode != 0)
                            {
                                addDiagLine(
                                    $@"MassEffectModderNoGuiexited exited texture map consistency check with code {exitcode}",
                                    Severity.ERROR);
                            }

                            if (removedFiles.Any())
                            {
                                addDiagLine(@"The following problems were detected checking game consistency with the texture map file:", Severity.ERROR);
                                foreach (var error in removedFiles)
                                {
                                    addDiagLine(@" - " + error, Severity.ERROR);
                                }
                            }

                            if (addedFiles.Any())
                            {
                                addDiagLine(@"The following files were added after textures were installed:", Severity.ERROR);
                                foreach (var error in addedFiles)
                                {
                                    addDiagLine(@" - " + error, Severity.ERROR);
                                }
                            }

                            if (replacedFiles.Any())
                            {
                                addDiagLine(@"The following files were replaced after textures were installed:", Severity.ERROR);
                                foreach (var error in replacedFiles)
                                {
                                    addDiagLine(@" - " + error, Severity.ERROR);
                                }
                            }

                            if (replacedFiles.Any() || addedFiles.Any() || removedFiles.Any())
                            {
                                addDiagLine(@"Diagnostic detected that some files were added, removed or replaced after textures were installed.", Severity.ERROR);
                                addDiagLine(@"Package files cannot be installed after a texture mod is installed - the texture pointers will be wrong.", Severity.ERROR);
                            }
                            else
                            {
                                addDiagLine(@"Diagnostic reports no files appear to have been added or removed since texture scan took place.");
                            }

                        }
                        else
                        {
                            addDiagLine($@"Texture map file is missing: {package.DiagnosticTarget.Game.ToString().ToLower()}map.bin - was game migrated to new system or are you M3 on a different user account?");
                        }
                    }

                    #endregion

                    #region Textures - full check
                    //FULL CHECK
                    if (package.PerformFullTexturesCheck)
                    {
                        MLog.Information(@"Performing full texture check");
                        var param = 0;
                        package.UpdateStatusCallback?.Invoke(LC.GetString(LC.string_interp_performingFullTexturesCheckX, param)); //done this way to save a string in localization
                        addDiagLine(@"Full Textures Check", Severity.DIAGSECTION);
                        args = $@"--check-game-data-textures --gameid {gameID} --ipc";
                        var emptyMipsNotRemoved = new List<string>();
                        var badTFCReferences = new List<string>();
                        var scanErrors = new List<string>();
                        string lastMissingTFC = null;
                        package.UpdateProgressCallback?.Invoke(0);
                        package.UpdateTaskbarProgressStateCallback?.Invoke(MTaskbarState.Progressing);

                        string currentProcessingFile = null;
                        void handleIPC(string command, string param)
                        {
                            switch (command)
                            {
                                case @"ERROR_MIPMAPS_NOT_REMOVED":
                                    if (package.DiagnosticTarget.TextureModded)
                                    {
                                        //only matters when game is texture modded
                                        emptyMipsNotRemoved.Add(param);
                                    }
                                    break;
                                case @"TASK_PROGRESS":
                                    if (int.TryParse(param, out var progress))
                                    {
                                        package.UpdateProgressCallback?.Invoke(progress);
                                    }
                                    package.UpdateStatusCallback?.Invoke(LC.GetString(LC.string_interp_performingFullTexturesCheckX, param));
                                    break;
                                case @"PROCESSING_FILE":
                                    // Print this out if MEM dies
                                    currentProcessingFile = param;
                                    break;
                                case @"ERROR_REFERENCED_TFC_NOT_FOUND":
                                    //badTFCReferences.Add(param);
                                    lastMissingTFC = param;
                                    break;
                                case @"ERROR_TEXTURE_SCAN_DIAGNOSTIC":
                                    if (lastMissingTFC != null)
                                    {
                                        if (lastMissingTFC.StartsWith(@"Textures_"))
                                        {
                                            var foldername = Path.GetFileNameWithoutExtension(lastMissingTFC).Substring(@"Textures_".Length);
                                            if (MEDirectories.OfficialDLC(package.DiagnosticTarget.Game)
                                                .Contains(foldername))
                                            {
                                                break; //dlc is packed still
                                            }
                                        }
                                        badTFCReferences.Add(lastMissingTFC + @", " + param);
                                    }
                                    else
                                    {
                                        scanErrors.Add(param);
                                    }
                                    lastMissingTFC = null; //reset
                                    break;
                                default:
                                    Debug.WriteLine($@"{command} {param}");
                                    break;
                            }
                        }

                        string memCrashText = null;
                        MEMIPCHandler.RunMEMIPCUntilExit(package.DiagnosticTarget.Game.IsOTGame(),
                            args,
                            ipcCallback: handleIPC,
                            applicationExited: x => exitcode = x,
                            setMEMCrashLog: x => memCrashText = x
                        );

                        if (exitcode != 0)
                        {
                            addDiagLine($@"MassEffectModderNoGui exited full textures check with code {exitcode}", Severity.ERROR);
                            if (currentProcessingFile != null)
                            {
                                addDiagLine($@"The last file processed by MassEffectModder was: {currentProcessingFile}", Severity.ERROR);
                            }
                            addDiagLine();
                        }

                        package.UpdateProgressCallback?.Invoke(0);
                        package.UpdateTaskbarProgressStateCallback?.Invoke(MTaskbarState.Indeterminate);


                        if (emptyMipsNotRemoved.Any() || badTFCReferences.Any() || scanErrors.Any())
                        {
                            addDiagLine(@"Texture check reported errors", Severity.ERROR);
                            if (emptyMipsNotRemoved.Any())
                            {
                                addDiagLine();
                                addDiagLine(@"The following textures contain empty mips, which typically means files were installed after texture mods were installed.:", Severity.ERROR);
                                foreach (var em in emptyMipsNotRemoved)
                                {
                                    addDiagLine(@" - " + em, Severity.ERROR);
                                }
                            }

                            if (badTFCReferences.Any())
                            {
                                addDiagLine();
                                addDiagLine(@"The following textures have bad TFC references, which means the mods were built wrong, dependent DLC is missing, or the mod was installed wrong:", Severity.ERROR);
                                foreach (var br in badTFCReferences)
                                {
                                    addDiagLine(@" - " + br, Severity.ERROR);
                                }
                            }

                            if (scanErrors.Any())
                            {
                                addDiagLine();
                                addDiagLine(@"The following textures failed to scan:", Severity.ERROR);
                                foreach (var fts in scanErrors)
                                {
                                    addDiagLine(@" - " + fts, Severity.ERROR);
                                }
                            }
                        }
                        else if (exitcode != 0)
                        {
                            addDiagLine(@"Texture check failed");
                            if (memCrashText != null)
                            {
                                addDiagLine(@"MassEffectModder crashed with info:");
                                addDiagLines(memCrashText.Split("\n"), Severity.ERROR); //do not localize
                            }
                        }
                        else
                        {
                            // Is this right?? We skipped check. We can't just print this
                            addDiagLine(@"Texture check did not find any texture issues in this installation");
                        }
                    }
                    #endregion

                    #region Texture LODs
                    MLog.Information(@"Collecting texture LODs");

                    package.UpdateStatusCallback?.Invoke(@"Collecting LOD settings");
                    var lods = MEMIPCHandler.GetLODs(package.DiagnosticTarget.Game);
                    if (lods != null)
                    {
                        addLODStatusToDiag(package.DiagnosticTarget, lods, addDiagLine);
                    }
                    else
                    {
                        addDiagLine(@"MassEffectModderNoGui exited --print-lods with error. See application log for more info.", Severity.ERROR);
                    }

                    #endregion
                }
                else
                {
                    MLog.Warning(@"MEM not available. Multiple collections were skipped");

                    addDiagLine(@"Texture checks skipped", Severity.DIAGSECTION);
                    addDiagLine(@"Mass Effect Modder No Gui was not available for use when this diagnostic was run.", Severity.WARN);
                    addDiagLine(@"The following checks were skipped:", Severity.WARN);
                    addDiagLine(@" - Files added or removed after texture install", Severity.WARN);
                    addDiagLine(@" - Blacklisted mods check", Severity.WARN);
                    addDiagLine(@" - Textures check", Severity.WARN);
                    addDiagLine(@" - Texture LODs check", Severity.WARN);
                }

                #region ASI mods
                MLog.Information(@"Collecting ASI mod information");

                package.UpdateStatusCallback?.Invoke(LC.GetString(LC.string_collectingASIFileInformation));

                string asidir = M3Directories.GetASIPath(package.DiagnosticTarget);
                addDiagLine(@"Installed ASI mods", Severity.DIAGSECTION);
                if (Directory.Exists(asidir))
                {
                    addDiagLine(@"The following ASI files are located in the ASI directory:");
                    string[] files = Directory.GetFiles(asidir, @"*.asi");
                    if (!files.Any())
                    {
                        addDiagLine(@"ASI directory is empty. No ASI mods are installed.");
                    }
                    else
                    {
                        var installedASIs = package.DiagnosticTarget.GetInstalledASIs();
                        var nonUniqueItems = installedASIs.OfType<KnownInstalledASIMod>().SelectMany(
                            x => installedASIs.OfType<KnownInstalledASIMod>().Where(
                                y => x != y
                                     && x.AssociatedManifestItem.OwningMod ==
                                     y.AssociatedManifestItem.OwningMod)
                            ).Distinct().ToList();

                        foreach (var knownAsiMod in installedASIs.OfType<KnownInstalledASIMod>().Except(nonUniqueItems))
                        {
                            var str = $@" - {knownAsiMod.AssociatedManifestItem.Name} v{knownAsiMod.AssociatedManifestItem.Version} ({Path.GetFileName(knownAsiMod.InstalledPath)})";
                            if (knownAsiMod.Outdated)
                            {
                                str += @" - Outdated";
                            }
                            addDiagLine(str, knownAsiMod.Outdated ? Severity.WARN : Severity.GOOD);
                        }

                        foreach (var unknownAsiMod in installedASIs.OfType<UnknownInstalledASIMod>())
                        {
                            addDiagLine($@" - {Path.GetFileName(unknownAsiMod.InstalledPath)} - Unknown ASI mod", Severity.WARN);
                        }

                        foreach (var duplicateItem in nonUniqueItems)
                        {
                            var str = $@" - {duplicateItem.AssociatedManifestItem.Name} v{duplicateItem.AssociatedManifestItem.Version} ({Path.GetFileName(duplicateItem.InstalledPath)})";
                            if (duplicateItem.Outdated)
                            {
                                str += @" - Outdated";
                            }

                            str += @" - DUPLICATE ASI";
                            addDiagLine(str, Severity.FATAL);
                        }

                        addDiagLine();
                        addDiagLine(@"Ensure that only one version of an ASI is installed. If multiple copies of the same one are installed, the game may crash on startup.");
                    }
                }
                else
                {
                    addDiagLine(@"ASI directory does not exist. No ASI mods are installed.");
                }

                #endregion

                #region ME3/LE: TOC check

                //TOC SIZE CHECK
                if (package.DiagnosticTarget.Game == MEGame.ME3 || package.DiagnosticTarget.Game.IsLEGame())
                {
                    MLog.Information(@"Collecting TOC information");

                    package.UpdateStatusCallback?.Invoke(@"Collecting TOC file information");

                    addDiagLine(@"File Table of Contents (TOC) size check", Severity.DIAGSECTION);
                    addDiagLine(@"PCConsoleTOC.bin files list the size of each file the game can load.");
                    addDiagLine(@"If the size is smaller than the actual file, the game will not allocate enough memory to load the file.");
                    addDiagLine(@"These hangs typically occur at loading screens and are the result of manually modifying files without running AutoTOC afterwards.");
                    bool hadTocError = false;
                    string[] tocs = Directory.GetFiles(Path.Combine(gamePath, @"BIOGame"), @"PCConsoleTOC.bin", SearchOption.AllDirectories);
                    string markerfile = M3Directories.GetTextureMarkerPath(package.DiagnosticTarget);
                    foreach (string toc in tocs)
                    {
                        MLog.Information($@"Checking TOC file {toc}");

                        TOCBinFile tbf = new TOCBinFile(toc);
                        foreach (TOCBinFile.Entry ent in tbf.GetAllEntries())
                        {
                            //Console.WriteLine(index + "\t0x" + ent.offset.ToString("X6") + "\t" + ent.size + "\t" + ent.name);
                            var tocrootPath = gamePath;
                            if (Path.GetFileName(Directory.GetParent(toc).FullName).StartsWith(@"DLC_"))
                            {
                                tocrootPath = Directory.GetParent(toc).FullName;
                            }
                            string filepath = Path.Combine(tocrootPath, ent.name);
                            var fileExists = File.Exists(filepath);
                            if (fileExists)
                            {
                                if (!filepath.Equals(markerfile, StringComparison.InvariantCultureIgnoreCase) && !filepath.ToLower().EndsWith(@"pcconsoletoc.bin"))
                                {
                                    FileInfo fi = new FileInfo(filepath);
                                    long size = fi.Length;
                                    if (ent.size < size)
                                    {
                                        addDiagLine($@" - {filepath} size is {size}, but TOC lists {ent.size} ({ent.size - size} bytes)", Severity.ERROR);
                                        hadTocError = true;
                                    }
                                }
                            }
                            else
                            {
                                addDiagLine($@" - {filepath} is listed in TOC but is not present on disk", Severity.WARN);
                            }
                        }
                    }

                    if (!hadTocError)
                    {
                        addDiagLine(@"All TOC files passed check. No files have a size larger than the TOC size.");
                    }
                    else
                    {
                        addDiagLine(@"Some files are larger than the listed TOC size. This typically won't happen unless you manually installed some files or an ALOT installation failed.", Severity.ERROR);
                        addDiagLine(@"The game will always hang while loading these files." + (package.DiagnosticTarget.Supported ? @" You can regenerate the TOC files by using AutoTOC from the tools menu. If installation failed due a crash, this won't fix it." : ""));
                    }
                }

                #endregion

                #region Mass Effect (1) log files

                //ME1: LOGS
                if (package.DiagnosticTarget.Game == MEGame.ME1)
                {
                    MLog.Information(@"Collecting ME1 crash logs");

                    package.UpdateStatusCallback?.Invoke(LC.GetString(LC.string_collectingME1ApplicationLogs));

                    //GET LOGS
                    string logsdir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), @"BioWare\Mass Effect\Logs");
                    if (Directory.Exists(logsdir))
                    {
                        DirectoryInfo info = new DirectoryInfo(logsdir);
                        FileInfo[] files = info.GetFiles().Where(f => f.LastWriteTime > DateTime.Now.AddDays(-3)).OrderByDescending(p => p.LastWriteTime).ToArray();
                        DateTime threeDaysAgo = DateTime.Now.AddDays(-3);
                        foreach (FileInfo file in files)
                        {
                            //Console.WriteLine(file.Name + " " + file.LastWriteTime);
                            var logLines = File.ReadAllLines(file.FullName);
                            int crashLineNumber = -1;
                            int currentLineNumber = -1;
                            string reason = "";
                            foreach (string line in logLines)
                            {
                                if (line.Contains(@"Critical: appError called"))
                                {
                                    crashLineNumber = currentLineNumber;
                                    reason = @"Log file indicates crash occurred";
                                    MLog.Information(@"Found crash in ME1 log " + file.Name + @" on line " + currentLineNumber);
                                    break;
                                }

                                currentLineNumber++;
                            }

                            if (crashLineNumber >= 0)
                            {
                                crashLineNumber = Math.Max(0, crashLineNumber - 10); //show last 10 lines of log leading up to the crash
                                                                                     //this log has a crash
                                addDiagLine(@"Mass Effect game log " + file.Name, Severity.DIAGSECTION);
                                if (reason != "") addDiagLine(reason);
                                if (crashLineNumber > 0)
                                {
                                    addDiagLine(@"[CRASHLOG]...");
                                }

                                for (int i = crashLineNumber; i < logLines.Length; i++)
                                {
                                    addDiagLine(@"[CRASHLOG]" + logLines[i]);
                                }
                            }
                        }
                    }
                }

                #endregion

                #region Event logs for crashes

                //EVENT LOGS
                MLog.Information(@"Collecting event logs");
                package.UpdateStatusCallback?.Invoke(LC.GetString(LC.string_collectingEventLogs));
                StringBuilder crashLogs = new StringBuilder();
                var sevenDaysAgo = DateTime.Now.AddDays(-3);

                //Get event logs
                EventLog ev = new EventLog(@"Application");
                List<EventLogEntry> entries = ev.Entries
                    .Cast<EventLogEntry>()
                    .Where(z => z.InstanceId == 1001 && z.TimeGenerated > sevenDaysAgo && (GenerateEventLogString(z).ContainsAny(MEDirectories.ExecutableNames(package.DiagnosticTarget.Game), StringComparison.InvariantCultureIgnoreCase)))
                    .ToList();

                addDiagLine($@"{package.DiagnosticTarget.Game.ToGameName()} crash logs found in Event Viewer", Severity.DIAGSECTION);
                if (entries.Any())
                {
                    foreach (var entry in entries)
                    {
                        string str = string.Join("\n", GenerateEventLogString(entry).Split('\n').ToList().Take(17).ToList()); //do not localize
                        addDiagLine($"{package.DiagnosticTarget.Game.ToGameName()} Event {entry.TimeGenerated}\n{str}"); //do not localize
                    }

                }
                else
                {
                    addDiagLine(@"No crash events found in Event Viewer");
                }

                #endregion

                #region ME3Logger

                if (package.DiagnosticTarget.Game == MEGame.ME3)
                {
                    MLog.Information(@"Collecting ME3Logger session log");
                    package.UpdateStatusCallback?.Invoke(LC.GetString(LC.string_collectingME3SessionLog));
                    string me3logfilepath = Path.Combine(Directory.GetParent(M3Directories.GetExecutablePath(package.DiagnosticTarget)).FullName, @"me3log.txt");
                    if (File.Exists(me3logfilepath))
                    {
                        FileInfo fi = new FileInfo(me3logfilepath);
                        addDiagLine(@"Mass Effect 3 last session log", Severity.DIAGSECTION);
                        addDiagLine(@"Last session log has modification date of " + fi.LastWriteTimeUtc.ToShortDateString());
                        addDiagLine(@"Note that messages from this log can be highly misleading as they are context dependent!");
                        addDiagLine();
                        var log = MUtilities.WriteSafeReadAllLines(me3logfilepath); //try catch needed?
                        int lineNum = 0;
                        foreach (string line in log)
                        {
                            addDiagLine(line, line.Contains(@"I/O failure", StringComparison.InvariantCultureIgnoreCase) ? Severity.FATAL : Severity.INFO);
                            lineNum++;
                            if (lineNum > 100)
                            {
                                break;
                            }
                        }

                        if (lineNum > 200)
                        {
                            addDiagLine(@"... log truncated ...");
                        }
                    }
                }

                #endregion

                #region DebugLogger (LE)

                if (package.DiagnosticTarget.Game == MEGame.ME3)
                {
                    MLog.Information(@"Collecting DebugLogger session log");
                    package.UpdateStatusCallback?.Invoke(LC.GetString(LC.string_collectingME3SessionLog)); // NEEDS UPDATED
                    string me3logfilepath = Path.Combine(Directory.GetParent(M3Directories.GetExecutablePath(package.DiagnosticTarget)).FullName, @"DebugLogger.txt");
                    if (File.Exists(me3logfilepath))
                    {
                        FileInfo fi = new FileInfo(me3logfilepath);
                        addDiagLine($@"{package.DiagnosticTarget.Game.ToGameName()} last session log", Severity.DIAGSECTION);
                        addDiagLine(@"Last session log has modification date of " + fi.LastWriteTimeUtc.ToShortDateString());
                        addDiagLine(@"Note that messages from this log can be highly misleading. Do not try to interpret these messages if you don't know what they mean!");
                        addDiagLine();
                        var log = MUtilities.WriteSafeReadAllLines(me3logfilepath); //try catch needed?
                                                                                    //log = log.Reverse().ToArray(); // go in reverse so we get the last lines
                        int lineNum = 0;
                        foreach (string line in log)
                        {

                            if (package.DiagnosticTarget.Game == MEGame.LE2)
                            {
                                if (line.Contains("Material"))
                                {
                                    continue; // These are vanilla and should be ignored
                                }
                            }

                            addDiagLine(line, line.Contains(@"appErrorF", StringComparison.InvariantCultureIgnoreCase) ? Severity.FATAL : Severity.INFO);
                            lineNum++;
                            if (lineNum > 100)
                            {
                                break;
                            }
                        }

                        if (lineNum > 200)
                        {
                            addDiagLine(@"... log truncated ...");
                        }
                    }
                }

                #endregion
            }
            catch (Exception ex)
            {
                addDiagLine(@"Exception occurred while running diagnostic.", Severity.ERROR);
                addDiagLine(ex.FlattenException(), Severity.ERROR);
                return diagStringBuilder.ToString();
            }
            finally
            {
                //restore MEM setting
                // This is M3 specific
                if (hasMEM)
                {
                    if (File.Exists(_iniPath))
                    {
                        DuplicatingIni ini = DuplicatingIni.LoadIni(_iniPath);
                        ini[@"GameDataPath"][package.DiagnosticTarget.Game.ToString()].Value = oldMemGamePath;
                        File.WriteAllText(_iniPath, ini.ToString());
                    }
                }
            }

            return diagStringBuilder.ToString();
        }

        private static void SeeIfIncompatibleDLCIsInstalled(GameTarget target, Action<string, Severity> addDiagLine)
        {
            var installedDLCMods = VanillaDatabaseService.GetInstalledDLCMods(target);
            var metaFiles = target.GetMetaMappedInstalledDLC(false);

            foreach (var v in metaFiles)
            {
                if (v.Value != null && v.Value.IncompatibleDLC.Any())
                {
                    // See if any DLC is not compatible
                    var installedIncompatDLC = installedDLCMods.Intersect(v.Value.IncompatibleDLC, StringComparer.InvariantCultureIgnoreCase).ToList();
                    foreach (var id in installedIncompatDLC)
                    {
                        var incompatName = TPMIService.GetThirdPartyModInfo(id, target.Game);
                        addDiagLine($@"{v.Value.ModName} is not compatible with {incompatName?.modname ?? id}", Severity.FATAL);
                    }
                }
            }
        }

        private static void addLODStatusToDiag(GameTarget selectedDiagnosticTarget, Dictionary<string, string> lods, Action<string, Severity> addDiagLine)
        {
            addDiagLine(@"Texture Level of Detail (LOD) settings", Severity.DIAGSECTION);
            string iniPath = M3Directories.GetLODConfigFile(selectedDiagnosticTarget);
            if (!File.Exists(iniPath))
            {
                addDiagLine($@"Game config file is missing: {iniPath}", Severity.ERROR);
                return;
            }

            foreach (KeyValuePair<string, string> kvp in lods)
            {
                addDiagLine($@"{kvp.Key}={kvp.Value}", Severity.INFO);
            }

            var textureChar1024 = lods.FirstOrDefault(x => x.Key == @"TEXTUREGROUP_Character_1024");
            if (string.IsNullOrWhiteSpace(textureChar1024.Key)) //does this work for ME2/ME3??
            {
                //not found
                addDiagLine(@"Could not find TEXTUREGROUP_Character_1024 in config file for checking LOD settings", Severity.ERROR);
                return;
            }

            try
            {
                int maxLodSize = 0;
                if (!string.IsNullOrWhiteSpace(textureChar1024.Value))
                {
                    //ME2,3 default to blank
                    maxLodSize = int.Parse(StringStructParser.GetCommaSplitValues(textureChar1024.Value)[selectedDiagnosticTarget.Game == MEGame.ME1 ? @"MinLODSize" : @"MaxLODSize"]);
                }

                // Texture mod installed, missing HQ LODs
                var HQSettingsMissingLine = @"High quality texture LOD settings appear to be missing, but a high resolution texture mod appears to be installed.\n[ERROR]The game will not use these new high quality assets - config file was probably deleted or texture quality settings were changed in game"; //do not localize

                // No texture mod, no HQ LODs
                var HQVanillaLine = @"High quality LOD settings are not set and no high quality texture mod is installed";
                switch (selectedDiagnosticTarget.Game)
                {
                    case MEGame.ME1:
                        if (maxLodSize != 1024) //ME1 Default
                        {
                            //LODS MODIFIED!
                            if (maxLodSize == 4096)
                            {
                                addDiagLine(@"LOD quality settings: 4K textures", Severity.INFO);
                            }
                            else if (maxLodSize == 2048)
                            {
                                addDiagLine(@"LOD quality settings: 2K textures", Severity.INFO);
                            }

                            //Not Default
                            if (selectedDiagnosticTarget.TextureModded)
                            {
                                addDiagLine(@"This installation appears to have a texture mod installed, so unused/empty mips are already removed", Severity.INFO);
                            }
                            else if (maxLodSize > 1024)
                            {
                                addDiagLine(@"Texture LOD settings appear to have been raised, but this installation has not been texture modded - game will likely have unused mip crashes.", Severity.FATAL);
                            }
                        }
                        else
                        {
                            //Default ME1 LODs
                            if (selectedDiagnosticTarget.TextureModded && selectedDiagnosticTarget.HasALOTOrMEUITM())
                            {
                                addDiagLine(HQSettingsMissingLine, Severity.ERROR);
                            }
                            else
                            {
                                addDiagLine(HQVanillaLine, Severity.INFO);
                            }
                        }

                        break;
                    case MEGame.ME2:
                    case MEGame.ME3:
                        if (maxLodSize != 0)
                        {
                            //Not vanilla, alot/meuitm
                            if (selectedDiagnosticTarget.TextureModded && selectedDiagnosticTarget.HasALOTOrMEUITM())
                            {
                                //addDiagLine(HQVanillaLine, Severity.INFO);
                                if (maxLodSize == 4096)
                                {
                                    addDiagLine(@"LOD quality settings: 4K textures", Severity.INFO);
                                }
                                else if (maxLodSize == 2048)
                                {
                                    addDiagLine(@"LOD quality settings: 2K textures", Severity.INFO);
                                }
                            }
                            else
                            {
                                //else if (selectedDiagnosticTarget.TextureModded) //not vanilla, but no MEM/MEUITM
                                //{
                                if (maxLodSize == 4096)
                                {
                                    addDiagLine(@"LOD quality settings: 4K textures (no high res mod installed)", Severity.WARN);
                                }
                                else if (maxLodSize == 2048)
                                {
                                    addDiagLine(@"LOD quality settings: 2K textures (no high res mod installed)", Severity.INFO);
                                }

                                //}
                                if (!selectedDiagnosticTarget.TextureModded)
                                {
                                    //no texture mod, but has set LODs
                                    addDiagLine(@"LODs have been explicitly set, but a texture mod is not installed - game may have black textures as empty mips may not be removed", Severity.WARN);
                                }
                            }
                        }
                        else //default
                        {
                            //alot/meuitm, but vanilla settings.
                            if (selectedDiagnosticTarget.TextureModded &&
                                selectedDiagnosticTarget.HasALOTOrMEUITM())
                            {
                                addDiagLine(HQSettingsMissingLine, Severity.ERROR);
                            }
                            else //no alot/meuitm, vanilla setting.
                            {
                                addDiagLine(HQVanillaLine, Severity.INFO);
                            }
                        }

                        break;
                }
            }
            catch (Exception e)
            {
                MLog.Error(@"Error checking LOD settings: " + e.Message);
                addDiagLine($@"Error checking LOD settings: {e.Message}", Severity.INFO);
            }
        }

        private static void diagPrintSignatures(FileInspector info, Action<string, Severity> addDiagLine)
        {
            foreach (var sig in info.GetSignatures())
            {
                var signingTime = sig.TimestampSignatures.FirstOrDefault()?.TimestampDateTime?.UtcDateTime;
                addDiagLine(@" > Signed on " + signingTime, Severity.INFO);

                foreach (var signChain in sig.AdditionalCertificates)
                {
                    try
                    {
                        var outStr = signChain.Subject.Substring(3); //remove CN=
                        outStr = outStr.Substring(0, outStr.IndexOf(','));
                        addDiagLine(@" >> Signed by " + outStr, Severity.INFO);
                    }
                    catch
                    {
                        addDiagLine(@"  >> Signed by " + signChain.Subject, Severity.INFO);
                    }
                }
            }
        }

        private static string GenerateEventLogString(EventLogEntry entry) =>
            $"Event type: {entry.EntryType}\nEvent Message: {entry.Message + entry}\nEvent Time: {entry.TimeGenerated.ToShortTimeString()}\nEvent {entry.UserName}\n"; //do not localize

        private static int GetPartitionDiskBackingType(string partitionLetter)
        {
            using (var partitionSearcher = new ManagementObjectSearcher(
                @"\\localhost\ROOT\Microsoft\Windows\Storage",
                $@"SELECT DiskNumber FROM MSFT_Partition WHERE DriveLetter='{partitionLetter}'"))
            {
                try
                {
                    var partition = partitionSearcher.Get().Cast<ManagementBaseObject>().Single();
                    using (var physicalDiskSearcher = new ManagementObjectSearcher(
                        @"\\localhost\ROOT\Microsoft\Windows\Storage",
                        $@"SELECT Size, Model, MediaType FROM MSFT_PhysicalDisk WHERE DeviceID='{ partition[@"DiskNumber"] }'")) //do not localize
                    {
                        var physicalDisk = physicalDiskSearcher.Get().Cast<ManagementBaseObject>().Single();
                        return
                            (UInt16)physicalDisk[@"MediaType"];/*||
                        SSDModelSubstrings.Any(substring => result.Model.ToLower().Contains(substring)); ;*/


                    }
                }
                catch (Exception e)
                {
                    MLog.Error($@"Error reading partition type on {partitionLetter}: {e.Message}");
                    return -1;
                }
            }
        }

        public static string GetProcessorInformationForDiag()
        {
            string str = "";
            try
            {
                ManagementObjectSearcher mosProcessor = new ManagementObjectSearcher(@"SELECT * FROM Win32_Processor");

                foreach (ManagementObject moProcessor in mosProcessor.Get())
                {
                    if (str != "")
                    {
                        str += "\n"; //do not localize
                    }

                    if (moProcessor[@"name"] != null)
                    {
                        str += moProcessor[@"name"].ToString();
                        str += "\n"; //do not localize
                    }
                    if (moProcessor[@"maxclockspeed"] != null)
                    {
                        str += @"Maximum reported clock speed: ";
                        str += moProcessor[@"maxclockspeed"].ToString();
                        str += " Mhz\n"; //do not localize
                    }
                    if (moProcessor[@"numberofcores"] != null)
                    {
                        str += @"Cores: ";

                        str += moProcessor[@"numberofcores"].ToString();
                        str += "\n"; //do not localize
                    }
                    if (moProcessor[@"numberoflogicalprocessors"] != null)
                    {
                        str += @"Logical processors: ";
                        str += moProcessor[@"numberoflogicalprocessors"].ToString();
                        str += "\n"; //do not localize
                    }

                }
                return str
                   .Replace(@"(TM)", @"™")
                   .Replace(@"(tm)", @"™")
                   .Replace(@"(R)", @"®")
                   .Replace(@"(r)", @"®")
                   .Replace(@"(C)", @"©")
                   .Replace(@"(c)", @"©")
                   .Replace(@"    ", @" ")
                   .Replace(@"  ", @" ").Trim();
            }
            catch (Exception e)
            {
                MLog.Error($@"Error getting processor information: {e.Message}");
                return $"Error getting processor information: {e.Message}\n"; //do not localize
            }
        }

        /// <summary>
        /// Log session divider. Should always be the very first line of a new session
        /// </summary>
        public static string SessionStartString { get; } = "============================SESSION START============================";

        /// <summary>
        /// Creates a document ready for upload to ME3Tweaks Log Viewing Service.
        /// </summary>
        /// <param name="selectedDiagnosticTarget"></param>
        /// <param name="selectedLog"></param>
        /// <param name="textureCheck"></param>
        /// <param name="updateStatusCallback"></param>
        /// <param name="updateProgressCallback"></param>
        /// <param name="updateTaskbarProgressStateCallback"></param>
        /// <returns></returns>
        public static string SubmitDiagnosticLog(LogUploadPackage package)
        {

            StringBuilder logUploadText = new StringBuilder();
            if (package.DiagnosticTarget != null && !package.DiagnosticTarget.IsCustomOption && (package.DiagnosticTarget.Game.IsOTGame() || package.DiagnosticTarget.Game.IsLEGame()))
            {
                Debug.WriteLine(@"Selected game target: " + package.DiagnosticTarget.TargetPath);
                logUploadText.Append("[MODE]diagnostics\n"); //do not localize
                logUploadText.Append(LogCollector.PerformDiagnostic(package));
                logUploadText.Append("\n"); //do not localize
            }

            if (package.SelectedLog != null && package.SelectedLog.Selectable)
            {
                Debug.WriteLine(@"Selected log: " + package.SelectedLog.filepath);
                logUploadText.Append("[MODE]logs\n"); //do not localize
                logUploadText.AppendLine(LogCollector.CollectLogs(package.SelectedLog.filepath));
                logUploadText.Append("\n"); //do not localize
            }

            var logtext = logUploadText.ToString();
            package.UpdateStatusCallback?.Invoke(LC.GetString(LC.string_compressingForUpload));
            var lzmalog = LZMA.CompressToLZMAFile(Encoding.UTF8.GetBytes(logtext));
            try
            {
                //this doesn't need to technically be async, but library doesn't have non-async method.
                //DEBUG ONLY!!!
                package.UpdateStatusCallback?.Invoke(LC.GetString(LC.string_uploadingToME3Tweaks));

                string responseString = package.UploadEndpoint.PostUrlEncodedAsync(new { LogData = Convert.ToBase64String(lzmalog), ToolName = MLibraryConsumer.GetHostingProcessname(), ToolVersion = MLibraryConsumer.GetAppVersion() }).ReceiveString().Result;
                Uri uriResult;
                bool result = Uri.TryCreate(responseString, UriKind.Absolute, out uriResult) && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
                if (result)
                {
                    //should be valid URL.
                    //diagnosticsWorker.ReportProgress(0, new ThreadCommand(SET_DIAGTASK_ICON_GREEN, Image_Upload));
                    //e.Result = responseString;
                    MLog.Information(@"Result from server for log upload: " + responseString);
                    return responseString;
                }
                else
                {
                    MLog.Error(@"Error uploading log. The server responded with: " + responseString);
                    return LC.GetString(LC.string_interp_serverRejectedTheUpload, responseString);
                }
            }
            catch (AggregateException e)
            {
                Exception ex = e.InnerException;
                string exmessage = ex.Message;
                return LC.GetString(LC.string_interp_logWasUnableToUpload, exmessage);
            }
            catch (FlurlHttpTimeoutException)
            {
                // FlurlHttpTimeoutException derives from FlurlHttpException; catch here only
                // if you want to handle timeouts as a special case
                MLog.Error(@"Request timed out while uploading log.");
                return LC.GetString(LC.string_interp_requestTimedOutUploading);

            }
            catch (Exception ex)
            {
                // ex.Message contains rich details, including the URL, verb, response status,
                // and request and response bodies (if available)
                MLog.Exception(ex, @"Handled error uploading log: ");
                string exmessage = ex.Message;
                var index = exmessage.IndexOf(@"Request body:");
                if (index > 0)
                {
                    exmessage = exmessage.Substring(0, index);
                }

                return LC.GetString(LC.string_interp_logWasUnableToUpload, exmessage);
            }
        }
    }

    /// <summary>
    /// Package of options for what to collect in a diagnostic/log upload.
    /// </summary>
    public class LogUploadPackage
    {
        /// <summary>
        /// Endpoint for uploading to. This is required.
        /// </summary>
        public string UploadEndpoint { get; set; }

        /// <summary>
        /// Target to perform diagnostic on (can be null)
        /// </summary>
        public GameTarget DiagnosticTarget { get; set; }

        /// <summary>
        /// Application log to upload (can be null)
        /// </summary>
        public LogItem SelectedLog { get; set; }

        /// <summary>
        /// If a full texture chekc should be performed. This only occurs if DiagnosticTarget is not null.
        /// </summary>
        public bool PerformFullTexturesCheck { get; set; }

        /// <summary>
        /// Invoked when the text status of the log collection should be updated.
        /// </summary>
        public Action<string> UpdateStatusCallback { get; set; }

        /// <summary>
        /// Invoked when a progress bar of some kind should be updated
        /// </summary>
        public Action<int> UpdateProgressCallback { get; set; }

        /// <summary>
        /// Invoked when a taskbar state should be updated (or progressbar)
        /// </summary>
        public Action<MTaskbarState> UpdateTaskbarProgressStateCallback { get; set; }
    }
}