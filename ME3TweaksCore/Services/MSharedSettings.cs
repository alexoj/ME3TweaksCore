using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ME3TweaksCore.Helpers;
using Microsoft.Win32;

namespace ME3TweaksCore.Services
{
    internal class MSharedSettings
    {
        private const string REGISTRY_KEY_ME3TWEAKS = @"HKEY_CURRENT_USER\Software\ME3Tweaks";

        public static void WriteSettingString(string valuename, string data)
        {
            RegistryHandler.WriteRegistryString(REGISTRY_KEY_ME3TWEAKS, valuename, data);
        }

        public static string GetSettingString(string valuename)
        {
            return RegistryHandler.GetRegistryString(REGISTRY_KEY_ME3TWEAKS, valuename);
        }

        public static void DeleteSetting(string valuename)
        {
            RegistryHandler.DeleteRegistryKey(REGISTRY_KEY_ME3TWEAKS, valuename);
        }


        // SETTINGS

        /// <summary>
        /// Timeout for ShortWebClient, in seconds.
        /// </summary>
        public static int WebClientTimeout { get; set; } = 5;

        /// <summary>
        /// The last time content was checked online. This is to gate off content refreshes.
        /// </summary>
        public static DateTime LastContentCheck { get; set; }
    }
}
