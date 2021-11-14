using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using ME3TweaksCore.Diagnostics;
using Serilog;

namespace ME3TweaksCore.Helpers
{
    class MUtilities
    {
        public static string CalculateMD5(string filename)
        {
            try
            {
                Debug.WriteLine("Hashing file " + filename);
                using var md5 = MD5.Create();
                using var stream = File.OpenRead(filename);
                var hash = md5.ComputeHash(stream);
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
            catch (IOException e)
            {
                MLog.Error("I/O ERROR CALCULATING CHECKSUM OF FILE: " + filename);
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
                MLog.Error("I/O ERROR CALCULATING CHECKSUM OF STREAM");
                MLog.Error(e);
                return "";
            }
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
            Log.Information("Extracting embedded file: " + internalResourceName + " to memory");
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
    }
}
