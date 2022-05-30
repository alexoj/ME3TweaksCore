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
        public static void WriteRegistryString(string subpath, string value, string data)
        {
            var subkey = CreateRegistryPath(subpath);
            subkey.SetValue(value, data);
        }

        /// <summary>
        /// Writes a string value to the registry. The path must start with HKEY_CURRENT_USER.
        /// </summary>
        /// <param name="subpath"></param>
        /// <param name="value"></param>
        /// <param name="data"></param>
        public static void WriteRegistryBool(string subpath, string value, bool data)
        {
            var subkey = CreateRegistryPath(subpath);
            subkey.SetValue(value, data ? 1 : 0, RegistryValueKind.DWord);
        }

        private static RegistryKey CreateRegistryPath(string subpath)
        {
            int i = 0;
            List<string> subkeys = subpath.Split('\\').ToList();
            RegistryKey subkey;
            if (subkeys[0] == @"HKEY_CURRENT_USER")
            {
                subkeys.RemoveAt(0);
                subkey = Registry.CurrentUser;
            }
            else
            {
                // This is dev only so we don't localize it
                throw new Exception(@"Currently only HKEY_CURRENT_USER keys are supported for writing.");
            }

            while (i < subkeys.Count)
            {
                subkey = subkey.CreateSubKey(subkeys[i]);
                i++;
            }

            return subkey;
        }

        /// <summary>
        /// Deletes a registry key from the registry. Only works with Registry.CurrentUser. USE WITH CAUTION
        /// </summary>
        /// <param name="primaryKey"></param>
        /// <param name="subkey"></param>
        /// <param name="valuename"></param>
        public static void DeleteRegistryKey(RegistryKey primaryKey, string subkey, string valuename)
        {
            using RegistryKey key = primaryKey.OpenSubKey(subkey, true);
            key?.DeleteValue(valuename, false);
        }

        /// <summary>
        /// Deletes a registry key from the registry. Only works with HKEY_CURRENT_USER (full or subkey paths only). USE WITH CAUTION
        /// </summary>
        /// <param name="primaryKey"></param>
        /// <param name="subkey"></param>
        /// <param name="valuename"></param>
        public static void DeleteRegistryKey(string fullkeypath, string valuename)
        {
            if (fullkeypath.StartsWith(@"HKEY_"))
            {
                if (!fullkeypath.StartsWith(@"HKEY_CURRENT_USER\"))
                {
                    throw new Exception(@"Cannot delete registry keys outside of HKEY_CURRENT_USER!");
                }
                else
                {
                    fullkeypath = fullkeypath.Substring(fullkeypath.IndexOf('\\') + 1);
                }
            }

            DeleteRegistryKey(Registry.CurrentUser, fullkeypath, valuename);
        }


        public static string GetRegistryString(string key, string valueName)
        {
            return (string)Registry.GetValue(key, valueName, null);
        }

        private static int GetRegistryInt(string key, string valueName)
        {
            return (int)Registry.GetValue(key, valueName, 0);
        }

        public static bool GetRegistryBool(string key, string valueName)
        {
            var val = GetRegistryInt(key, valueName);
            return val == 1;
        }

        
    }
}
