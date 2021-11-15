using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ME3TweaksCore.Diagnostics;
using ME3TweaksCore.Misc;

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
    }
}
