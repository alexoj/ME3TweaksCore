using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using Serilog;

namespace ME3TweaksCore
{
    /// <summary>
    /// Localization Core interposer
    /// </summary>
    public class LC
    {
        /// <summary>
        /// External localization resolver. Can be set via SetStringResolver
        /// </summary>
        private static Func<string, object[], string> StringResolver;

        public static void SetStringResolver(Func<string, object[], string> resolver)
        {
            StringResolver = resolver;
        }

        internal static string GetString(string resourceKey, params object[] interpolationItems)
        {
            if (StringResolver != null)
            {
                return StringResolver.Invoke(resourceKey, interpolationItems);
            }

            try
            {
                if (!resourceKey.StartsWith(@"string_")) throw new Exception(@"Localization keys must start with a string_ identifier!");
                var str = (string)MediaTypeNames.Application.Current.FindResource(resourceKey);
                str = str.Replace(@"\n", Environment.NewLine);
                return string.Format(str, interpolationItems);
            }
            catch (Exception e)
            {
                Log.Error($@"Error fetching string with key {resourceKey}: {e.ToString()}.");
                return $@"Error fetching string with key {resourceKey}: {e.ToString()}! Please report this to the developer";
            }
        }
    }
}
