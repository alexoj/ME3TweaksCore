using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using ME3TweaksCore.Diagnostics;
using ME3TweaksCore.Helpers;
using ME3TweaksCore.Localization;
using Serilog;

namespace ME3TweaksCore.Misc
{
    /// <summary>
    /// Helper class for copying a directory with progress
    /// Copied and modified from ALOT Installer
    /// </summary>
    public static class CopyTools
    {
        /// <summary>
        /// Copies a file using Webclient to provide progress callbacks with error handling.
        /// </summary>
        /// <param name="sourceFile"></param>
        /// <param name="destFile"></param>
        /// <param name="progressCallback"></param>
        /// <param name="errorCallback"></param>
        /// <returns></returns>
        public static bool CopyFileWithProgress(string sourceFile, string destFile, Action<long, long> progressCallback, Action<Exception> errorCallback)
        {
            using WebClient downloadClient = new WebClient();
            downloadClient.DownloadProgressChanged += (s, e) =>
            {
                progressCallback?.Invoke(e.BytesReceived, e.TotalBytesToReceive);
            };
            bool result = false;
            object syncObj = new object();
            downloadClient.DownloadFileCompleted += (s, e) =>
            {
                if (e.Error != null)
                {
                    MLog.Exception(e.Error, @"An error occurred copying the file to the destination:");
                    errorCallback?.Invoke(e.Error);
                }
                else if (File.Exists(destFile))
                {
                    result = true;
                }
                else
                {
                    MLog.Error($@"Destination file doesn't exist after file copy: {destFile}");
                    errorCallback?.Invoke(new Exception(LC.GetString(LC.string_interp_copyDidntWorkX, destFile)));
                }

                lock (syncObj)
                {
                    Monitor.Pulse(syncObj);
                }
            };
            downloadClient.DownloadFileTaskAsync(new Uri(sourceFile), destFile).ContinueWith(x =>
            {
                // Weird async exception handling
                if (x.Exception != null)
                {
                    throw x.Exception;
                }
            });
            lock (syncObj)
            {
                Monitor.Wait(syncObj);
            }
            return result;
        }


        public static int CopyAll_ProgressBar(DirectoryInfo source,
            DirectoryInfo target,
            Action<int> totalItemsToCopyCallback = null,
            Action fileCopiedCallback = null,
            Func<string, bool> aboutToCopyCallback = null,
            int total = -1,
            int done = 0,
            string[] ignoredExtensions = null,
            bool testrun = false,
            Action<string, long, long> bigFileProgressCallback = null,
            bool copyTimestamps = false,
            bool continueCopying = true)
        {
            if (total == -1 && continueCopying)
            {
                //calculate number of files
                total = Directory.GetFiles(source.FullName, @"*.*", SearchOption.AllDirectories).Length;
                totalItemsToCopyCallback?.Invoke(total);
            }

            int numdone = done;
            if (!testrun && continueCopying)
            {
                Directory.CreateDirectory(target.FullName);
            }

            // Copy each file into the new directory.
            foreach (FileInfo fi in source.GetFiles())
            {
                if (!continueCopying)
                    continue; // Skip em'
                if (ignoredExtensions != null)
                {
                    bool skip = false;
                    foreach (string str in ignoredExtensions)
                    {
                        if (fi.Name.ToLower().EndsWith(str))
                        {
                            skip = true;
                            break;
                        }
                    }
                    if (skip)
                    {
                        numdone++;
                        fileCopiedCallback?.Invoke();
                        continue;
                    }
                }

                string displayName = fi.Name;
                //if (path.ToLower().EndsWith(".sfar") || path.ToLower().EndsWith(".tfc"))
                //{
                //    long length = new System.IO.FileInfo(fi.FullName).Length;
                //    displayName += " (" + ByteSize.FromBytes(length) + ")";
                //}
                var shouldCopy = aboutToCopyCallback?.Invoke(fi.FullName);
                if (aboutToCopyCallback == null || (shouldCopy.HasValue && shouldCopy.Value))
                {
                    // This is a bad way of doing it
                    // ... but I don't care!!
                    Exception asyncException = null;

                    try
                    {

                        if (!testrun)
                        {
                            var destPath = Path.Combine(target.FullName, fi.Name);
                            if (bigFileProgressCallback != null && fi.Length > 1024 * 1024 * 128)
                            {
                                //128MB or bigger
                                CopyTools.CopyFileWithProgress(fi.FullName, destPath, 
                                    (bdone, btotal) => bigFileProgressCallback.Invoke(fi.FullName, bdone, btotal), 
                                    exception =>
                                    {
                                        continueCopying = false;
                                        asyncException = exception;
                                    });
                            }
                            else
                            {
                                // No big copy
                                fi.CopyTo(destPath, true);
                            }

                            if (continueCopying)
                            {
                                FileInfo dest = new FileInfo(destPath);
                                if (dest.IsReadOnly) dest.IsReadOnly = false;
                                if (copyTimestamps)
                                {
                                    MUtilities.CopyTimestamps(fi.FullName, destPath);
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        MLog.Error(@"Error copying file: " + fi + @" -> " + Path.Combine(target.FullName, fi.Name) + @": " + e.Message);
                        continueCopying = false;
                        throw;
                    }

                    if (asyncException != null)
                        throw asyncException; // Rethrow it here so it goes up
                }



                // MLog.Information(@"Copying {0}\{1}", target.FullName, fi.Name);
                numdone++;
                fileCopiedCallback?.Invoke();
            }

            // Copy each subdirectory using recursion.
            foreach (DirectoryInfo diSourceSubDir in source.GetDirectories())
            {
                if (continueCopying)
                {
                    DirectoryInfo nextTargetSubDir = testrun ? null : target.CreateSubdirectory(diSourceSubDir.Name);
                    numdone = CopyAll_ProgressBar(diSourceSubDir, nextTargetSubDir, totalItemsToCopyCallback,
                        fileCopiedCallback, aboutToCopyCallback, total, numdone, null, testrun, bigFileProgressCallback,
                        copyTimestamps, continueCopying);
                }
            }
            return numdone;
        }

        public static void CopyFiles_ProgressBar(Dictionary<string, string> fileMapping, Action<string> fileCopiedCallback = null, bool testrun = false)
        {
            foreach (var singleMapping in fileMapping)
            {
                var source = singleMapping.Key;
                var dest = singleMapping.Value;
                if (!testrun)
                {
                    //Will attempt to create dir, prompt for admin if necessary (not sure how this will work in the wild)
                    MUtilities.CreateDirectoryWithWritePermission(Directory.GetParent(dest).FullName);

                    if (File.Exists(dest))
                    {
                        FileAttributes attributes = File.GetAttributes(dest);

                        if ((attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                        {
                            // Make the file RW
                            attributes = attributes & ~FileAttributes.ReadOnly;
                            File.SetAttributes(dest, attributes);
                        }
                    }
                    FileInfo si = new FileInfo(source);
                    if (si.IsReadOnly)
                    {
                        si.IsReadOnly = false; //remove flag. Some mod archives do this I guess.
                    }
                    File.Copy(source, dest, true);
                }
                else
                {
                    FileInfo f = new FileInfo(source); //get source info. this will throw exception if an error occurs
                }

                fileCopiedCallback?.Invoke(dest);
            }
        }
    }
}
