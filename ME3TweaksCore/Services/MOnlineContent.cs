using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ME3TweaksCore.Diagnostics;
using ME3TweaksCore.Helpers;
using ME3TweaksCore.Misc;
using Serilog;

namespace ME3TweaksCore.Services
{
    public partial class MOnlineContent
    {
        /// <summary>
        /// Checks if we can perform an online content fetch. This value is updated when manually checking for content updates, and on automatic 1-day intervals (if no previous manual check has occurred)
        /// </summary>
        /// <returns></returns>
        internal static bool CanFetchContentThrottleCheck()
        {
            var lastContentCheck = MSharedSettings.LastContentCheck;
            var timeNow = DateTime.Now;
            return (timeNow - lastContentCheck).TotalDays > 1;
        }

        public static string FetchRemoteString(string url, string authorizationToken = null)
        {
            try
            {
                using var wc = new ShortTimeoutWebClient();
                if (authorizationToken != null)
                {
                    wc.Headers.Add(@"Authorization", authorizationToken);
                }
                return wc.DownloadStringAwareOfEncoding(url);
            }
            catch (Exception e)
            {
                MLog.Error(@"Error downloading string: " + e.Message);
                return null;
            }
        }

        /// <summary>
        /// Downloads from a URL to memory. This is a blocking call and must be done on a background thread.
        /// </summary>
        /// <param name="url">URL to download from</param>
        /// <param name="progressCallback">Progress information clalback</param>
        /// <param name="hash">Hash check value (md5). Leave null if no hash check</param>
        /// <returns></returns>

        public static async Task<(MemoryStream result, string errorMessage)> DownloadToMemory(string url,
            Action<long, long> progressCallback = null,
            string hash = null,
            bool logDownload = false,
            CancellationTokenSource cancellationTokenSource = null)
        {
            MemoryStream responseStream = new MemoryStream();
            string downloadError = null;

            using var wc = new HttpClientDownloadWithProgress(url, responseStream, cancellationTokenSource?.Token ?? default);
            wc.ProgressChanged += (totalFileSize, totalBytesDownloaded, progressPercentage) =>
            {
                progressCallback?.Invoke(totalBytesDownloaded, totalFileSize ?? 0);
            };

            if (logDownload)
            {
                Log.Information(@"Downloading to memory: " + url);
            }
            else
            {
                Debug.WriteLine("Downloading to memory: " + url);
            }


            wc.StartDownload().Wait();
            if (cancellationTokenSource != null && cancellationTokenSource.Token.IsCancellationRequested)
            {
                return (null, null);
            }

            if (hash == null) return (responseStream, downloadError);
            var md5 = MUtilities.CalculateMD5(responseStream);
            responseStream.Position = 0;
            if (md5 != hash)
            {
                responseStream = null;
                downloadError =
                    $"Hash of downloaded item ({url}) does not match expected hash. Expected: {hash}, got: {md5}"; //needs localized
            }

            return (responseStream, downloadError);
        }
    }
}
