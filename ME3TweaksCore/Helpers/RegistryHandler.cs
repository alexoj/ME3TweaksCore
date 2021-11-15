using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace ME3TweaksCore.Helpers
{
    public class RegistryHandler
    {
        /// <summary>
        /// Writes a string value to the registry. The path must start with HKEY_CURRENT_USER.
        /// </summary>
        /// <param name="subpath"></param>
        /// <param name="value"></param>
        /// <param name="data"></param>
        public static void WriteRegistryKey(string subpath, string value, string data)
        {
            int i = 0;
            List<string> subkeys = subpath.Split('\\').ToList();
            RegistryKey subkey;
            if (subkeys[0] == "HKEY_CURRENT_USER")
            {
                subkeys.RemoveAt(0);
                subkey = Registry.CurrentUser;
            }
            else
            {
                throw new Exception("Currently only HKEY_CURRENT_USER keys are supported for writing.");
            }

            while (i < subkeys.Count)
            {
                subkey = subkey.CreateSubKey(subkeys[i]);
                i++;
            }

            subkey.SetValue(value, data);
        }

        /// <summary>
        /// Deletes a registry key from the registry. USE WITH CAUTION
        /// </summary>
        /// <param name="primaryKey"></param>
        /// <param name="subkey"></param>
        /// <param name="valuename"></param>
        public static void DeleteRegistryKey(RegistryKey primaryKey, string subkey, string valuename)
        {
            using RegistryKey key = primaryKey.OpenSubKey(subkey, true);
            key?.DeleteValue(valuename, false);
        }

        public static string GetRegistryString(string key, string valueName)
        {
            return (string)Registry.GetValue(key, valueName, null);
        }
    }
}
