using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ME3TweaksCore.Helpers
{
    /// <summary>
    /// Contains filesystem methods to get and create directories and files used by ME3TweaksCore. Data is stored in the AppDataFolderName directory.
    /// </summary>
    public static class MCoreFilesystem
    {
        /// <summary>
        /// The appdata folder for ME3TweaksCore. Change this if you want to segregate your consuming application's data from other instances. CHANGE THIS BEFORE BOOTING THE LIBRARY.
        /// </summary>
        public static string AppDataFolderName { get; set; } = "ME3TweaksCore";

        /// <summary>
        /// Gets the ME3TweaksCore appdata directory.
        /// </summary>
        /// <param name="createIfMissing"></param>
        /// <returns></returns>
        internal static string GetME3TweaksCoreDataFolder(bool createIfMissing = true)
        {
            var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), AppDataFolderName);
            if (createIfMissing && !Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
            return folder;
        }

        /// <summary>
        /// Gets the directory that holds cached native dlls.
        /// </summary>
        /// <returns></returns>
        internal static string GetDllDirectory()
        {
            return Directory.CreateDirectory(Path.Combine(GetME3TweaksCoreDataFolder(), "dlls")).FullName;
        }

        /// <summary>
        /// Returns the path to where the cached online service responses reside on disk.
        /// </summary>
        /// <returns></returns>
        internal static string GetME3TweaksServicesCache()
        {
            return Directory.CreateDirectory(Path.Combine(GetME3TweaksCoreDataFolder(), "ME3TweaksServicesCache")).FullName;
        }

        /// <summary>
        /// Returns the path to where the cached local BasegameIdentificationService file resides on disk.
        /// </summary>
        /// <returns></returns>
        public static string GetLocalBasegameIdentificationServiceFile()
        {
            return Path.Combine(GetME3TweaksServicesCache(), "localbasegamefileidentificationservice.json");
        }

        /// <summary>
        /// Returns the path to where the cached server BasegameIdentificationService file resides on disk.
        /// </summary>
        /// <returns></returns>
        public static string GetME3TweaksBasegameFileIdentificationServiceFile()
        {
            return Path.Combine(GetME3TweaksServicesCache(), "me3tweaksbasegamefileidentificationservice.json");
        }

        public static string GetThirdPartyIdentificationCachedFile()
        {
            return Path.Combine(GetME3TweaksServicesCache(), "thirdpartyidentificationservice.json");
        }

        private static string GetMEMDir()
        {
            return Directory.CreateDirectory(Path.Combine(GetME3TweaksCoreDataFolder(), "MassEffectModder")).FullName;
        }

        /// <summary>
        /// Gets path to where the MEM executable should reside
        /// </summary>
        /// <param name="classicMem"></param>
        /// <returns></returns>
        public static string GetMEMNoGuiPath(bool classicMem)
        {
            if (classicMem)
                return Path.Combine(GetMEMDir(), "MassEffectModderNoGui.exe");
            
            return Path.Combine(GetMEMDir(), "MassEffectModderNoGui_LE.exe");
        }

        /// <summary>
        /// Temp directory. This is cleaned on every boot of the library
        /// </summary>
        /// <returns></returns>
        public static string GetTempDirectory()
        {
            return Directory.CreateDirectory(Path.Combine(GetME3TweaksCoreDataFolder(), "Temp")).FullName;
        }

        /// <summary>
        /// Gets directory where executables that must be run are stored (PermissionsGranter.exe for example)
        /// </summary>
        /// <returns></returns>
        public static string GetCachedExecutablesDirectory()
        {
            return Directory.CreateDirectory(Path.Combine(GetME3TweaksCoreDataFolder(), "CachedExecutables")).FullName;
        }

        /// <summary>
        /// Gets filepath where a cached executable would reside. This does not mean the file exists.
        /// </summary>
        /// <param name="executableName">Full executable name including the extension.</param>
        /// <returns></returns>
        public static string GetCachedExecutable(string executableName)
        {
            return Path.Combine(GetCachedExecutablesDirectory(), executableName);
        }
    }
}
