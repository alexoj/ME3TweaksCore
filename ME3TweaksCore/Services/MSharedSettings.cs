using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ME3TweaksCore.Helpers;
using Microsoft.Win32;

namespace ME3TweaksCore.Services
{
    class MSharedSettings
    {
        private const string REGISTRY_KEY_ME3TWEAKS = @"HKEY_CURRENT_USER\Software\ME3Tweaks";

        public static void WriteSettingString(string valuename, string data)
        {
            RegistryHandler.WriteRegistryKey(REGISTRY_KEY_ME3TWEAKS, valuename, data);
        }
    }
}
