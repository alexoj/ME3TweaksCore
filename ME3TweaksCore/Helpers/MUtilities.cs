using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LegendaryExplorerCore.GameFilesystem;
using LegendaryExplorerCore.Helpers;
using LegendaryExplorerCore.Packages;
using ME3TweaksCore.Diagnostics;
using Serilog;

namespace ME3TweaksCore.Helpers
{
    /// <summary>
    /// Utility methods for ME3TweaksCore
    /// </summary>
    public class MUtilities
    {
        public static string CalculateMD5(string filename)
        {
            try
            {
                Debug.WriteLine(@"Hashing file " + filename);
                using var md5 = MD5.Create();
                using var stream = File.OpenRead(filename);
                var hash = md5.ComputeHash(stream);
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
            catch (IOException e)
            {
                MLog.Error(@"I/O ERROR CALCULATING CHECKSUM OF FILE: " + filename);
                MLog.Error(e);
                return "";
            }
        }

        public static string CalculateMD5(Stream stream)
        {
            try
            {
                using var md5 = MD5.Create();
                stream.Position = 0;
                var hash = md5.ComputeHash(stream);
                stream.Position = 0; // reset stream
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
            catch (Exception e)
            {
                MLog.Error(@"I/O ERROR CALCULATING CHECKSUM OF STREAM");
                MLog.Error(e);
                return "";
            }
        }

        internal static List<string> GetListOfInstalledAV()
        {
            List<string> av = new List<string>();
            // for Windows Vista and above '\root\SecurityCenter2'
            using (var searcher = new ManagementObjectSearcher(@"\\" +
                                                               Environment.MachineName +
                                                               @"\root\SecurityCenter2",
                "SELECT * FROM AntivirusProduct"))
            {
                var searcherInstance = searcher.Get();
                foreach (var instance in searcherInstance)
                {
                    av.Add(instance["displayName"].ToString());
                }
            }

            return av;
        }

        public static bool IsWindows10OrNewer()
        {
            var os = Environment.OSVersion;
            return os.Platform == PlatformID.Win32NT &&
                   (os.Version.Major >= 10);
        }

        private static Stream GetResourceStream(string assemblyResource, Assembly assembly = null)
        {
            assembly ??= Assembly.GetExecutingAssembly();
#if DEBUG
            // For debugging
            var res = assembly.GetManifestResourceNames();
#endif
            return assembly.GetManifestResourceStream(assemblyResource);
        }

        internal static MemoryStream ExtractInternalFileToStream(string internalResourceName)
        {
            MLog.Information($@"Extracting embedded file: {internalResourceName} to memory");
#if DEBUG
            // This is for inspecting the list of files in debugger
            var resources = Assembly.GetExecutingAssembly().GetManifestResourceNames();
#endif
            using (Stream stream = GetResourceStream(internalResourceName))
            {
                MemoryStream ms = new MemoryStream();
                stream.CopyTo(ms);
                ms.Position = 0;
                return ms;
            }
        }


        internal static void ExtractInternalFile(string internalResourceName, string destination, bool overwrite)
        {
            if (!File.Exists(destination) || overwrite || new FileInfo(destination).Length == 0)
            {
                if (File.Exists(destination))
                {
                    FileInfo fi = new FileInfo(destination);
                    if (fi.IsReadOnly)
                    {
                        fi.IsReadOnly = false; //clear read only. might happen on some binkw32 in archives, maybe
                    }
                }
                MLog.Information($@"Writing internal asset to disk: {internalResourceName} -> {destination}");
                var resource = ExtractInternalFileToStream(internalResourceName);
                resource.WriteToFile(destination);
            }
            else
            {
                //MLog.Warning("File already exists. Not overwriting file.");
            }

        }

        // ME2 and ME3 have same exe names.
        private static (bool isRunning, DateTime lastChecked) le1RunningInfo = (false, DateTime.MinValue.AddSeconds(5));
        private static (bool isRunning, DateTime lastChecked) me1RunningInfo = (false, DateTime.MinValue.AddSeconds(5));
        private static (bool isRunning, DateTime lastChecked) me2RunningInfo = (false, DateTime.MinValue.AddSeconds(5));
        private static (bool isRunning, DateTime lastChecked) me3RunningInfo = (false, DateTime.MinValue.AddSeconds(5));
        private static (bool isRunning, DateTime lastChecked) leLauncherRunningInfo = (false, DateTime.MinValue.AddSeconds(5));


        private static int TIME_BETWEEN_PROCESS_CHECKS = 5;

        /// <summary>
        /// Determines if a specific game is running. This method only updates every 3 seconds due to the huge overhead it has
        /// </summary>
        /// <returns>True if running, false otherwise</returns>
        public static bool IsGameRunning(MEGame gameID)
        {
            (bool isRunning, DateTime lastChecked) runningInfo = (false, DateTime.MinValue.AddSeconds(5));
            switch (gameID)
            {
                case MEGame.ME1:
                    runningInfo = me1RunningInfo;
                    break;
                case MEGame.LE1:
                    runningInfo = le1RunningInfo;
                    break;
                case MEGame.LE2:
                case MEGame.ME2:
                    runningInfo = me2RunningInfo;
                    break;
                case MEGame.LE3:
                case MEGame.ME3:
                    runningInfo = me3RunningInfo;
                    break;
                case MEGame.LELauncher:
                    runningInfo = leLauncherRunningInfo;
                    break;
            }

            var time = runningInfo.lastChecked.AddSeconds(TIME_BETWEEN_PROCESS_CHECKS);
            //Debug.WriteLine(time + " vs " + DateTime.Now);
            if (time > DateTime.Now)
            {
                //Debug.WriteLine("CACHED");
                return runningInfo.isRunning; //cached
            }
            //Debug.WriteLine("IsRunning: " + gameID);

            var processNames = MEDirectories.ExecutableNames(gameID).Select(x => Path.GetFileNameWithoutExtension(x));
            runningInfo.isRunning = Process.GetProcesses().Any(x => processNames.Contains(x.ProcessName));
            runningInfo.lastChecked = DateTime.Now;
            switch (gameID)
            {
                case MEGame.ME1:
                    me1RunningInfo = runningInfo;
                    break;
                case MEGame.LE1:
                    le1RunningInfo = runningInfo;
                    break;
                case MEGame.ME2:
                case MEGame.LE2:
                    me2RunningInfo = runningInfo;
                    break;
                case MEGame.ME3:
                case MEGame.LE3:
                    me3RunningInfo = runningInfo;
                    break;
                case MEGame.LELauncher:
                    leLauncherRunningInfo = runningInfo;
                    break;
            }

            return runningInfo.isRunning;
        }

        /// <summary>
        /// Deletes the contents of the specified folder, as well as the directory itself unless deleteDirectoryItself = false
        /// </summary>
        /// <param name="targetDirectory"></param>
        /// <param name="throwOnFailed"></param>
        /// <returns></returns>
        public static bool DeleteFilesAndFoldersRecursively(string targetDirectory, bool throwOnFailed = false, bool deleteDirectoryItself = true)
        {
            if (!Directory.Exists(targetDirectory))
            {
                Debug.WriteLine(@"Directory to delete doesn't exist: " + targetDirectory);
                return true;
            }

            bool result = true;
            foreach (string file in Directory.GetFiles(targetDirectory))
            {
                File.SetAttributes(file, FileAttributes.Normal); //remove read only
                try
                {
                    //Debug.WriteLine("Deleting file: " + file);
                    File.Delete(file);
                }
                catch (Exception e)
                {
                    MLog.Error($@"Unable to delete file: {file}. It may be open still: {e.Message}");
                    if (throwOnFailed)
                    {
                        throw;
                    }
                    return false;
                }
            }

            foreach (string subDir in Directory.GetDirectories(targetDirectory))
            {
                result &= DeleteFilesAndFoldersRecursively(subDir, throwOnFailed, true);
            }

            if (deleteDirectoryItself)
            {
                Thread.Sleep(10); // This makes the difference between whether it works or not. Sleep(0) is not enough.
                try
                {
                    Directory.Delete(targetDirectory);
                }
                catch (Exception e)
                {
                    MLog.Error($@"Unable to delete directory: {targetDirectory}. It may be open still or may not be actually empty: {e.Message}");
                    if (throwOnFailed)
                    {
                        throw;
                    }
                    return false;
                }
            }

            return result;
        }

        /// <summary>
        /// Recursively deletes all empty subdirectories.
        /// </summary>
        /// <param name="startLocation"></param>
        public static void DeleteEmptySubdirectories(string startLocation)
        {
            foreach (var directory in Directory.GetDirectories(startLocation))
            {
                DeleteEmptySubdirectories(directory);
                if (!Directory.EnumerateFileSystemEntries(directory).Any())
                {
                    Log.Information("Deleting empty directory: " + directory);
                    Directory.Delete(directory, false);
                }
            }
        }

        /// <summary> Checks for write access for the given file.
        /// </summary>
        /// <param name="fileName">The filename.</param>
        /// <returns>true, if write access is allowed, otherwise false</returns>
        public static bool IsDirectoryWritable(string dir)
        {
            try
            {
                System.IO.File.Create(Path.Combine(dir, @"temp_m3.txt")).Close();
                System.IO.File.Delete(Path.Combine(dir, @"temp_m3.txt"));
                return true;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
            catch (Exception e)
            {
                MLog.Error(@"Error checking permissions to folder: " + dir);
                MLog.Error(@"Directory write test had error that was not UnauthorizedAccess: " + e.Message);
            }

            return false;
        }

        /// <summary>
        /// Reads all lines from a file, attempting to do so even if the file is in use by another process
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string[] WriteSafeReadAllLines(String path)
        {
            using var csv = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var sr = new StreamReader(csv);
            List<string> file = new List<string>();
            while (!sr.EndOfStream)
            {
                file.Add(sr.ReadLine());
            }

            return file.ToArray();
        }

        public static MEGame GetGameFromNumber(string gameNum)
        {
            return GetGameFromNumber(int.Parse(gameNum));
        }

        /// <summary>
        /// Converts server game ID to Enum
        /// </summary>
        /// <param name="number"></param>
        /// <returns></returns>
        public static MEGame GetGameFromNumber(int number) => number switch
        {
            1 => MEGame.ME1,
            2 => MEGame.ME2,
            3 => MEGame.ME3,
            4 => MEGame.LE1,
            5 => MEGame.LE2,
            6 => MEGame.LE3,
            7 => MEGame.LELauncher,
            _ => MEGame.Unknown
        };

        /// <summary>
        /// Gets an integer percent based on the progress to a maximum. The progress value is rounded to the nearest integer.
        /// </summary>
        /// <param name="downloaded"></param>
        /// <param name="total"></param>
        /// <returns></returns>
        public static int GetPercent(long downloaded, long total)
        {
            return (int) Math.Round(downloaded * 100.0 / total);
        }
    }
}
