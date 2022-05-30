using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using ME3TweaksCore.Services;

namespace ME3TweaksCore.Misc
{
    public class ShortTimeoutWebClient : WebClient
    {
        protected override WebRequest GetWebRequest(Uri uri)
        {
            var w = base.GetWebRequest(uri) as HttpWebRequest;
            w.Headers.Add(HttpRequestHeader.AcceptEncoding, @"gzip,deflate"); // We accept gzip, deflate
            w.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
            w.Timeout = MSharedSettings.WebClientTimeout * 1000;
            return w;
        }
    }
}
