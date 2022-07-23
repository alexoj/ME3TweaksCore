using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ME3TweaksCore.Misc
{
    public static class ProperVersion
    {
        /// <summary>
        /// Parses a version number and sets all non-defined versions to 0, not -1.
        /// </summary>
        /// <param name="str"></param>
        /// <param name="version"></param>
        /// <returns></returns>
        private static Version ParseProperVersion(string versionStr)
        {
            Version v = new Version(versionStr);
            if (v.Build == -1) v = new Version(v.Major, v.Minor, 0, 0);
            else if (v.Revision == -1) v = new Version(v.Major, v.Minor, v.Build, 0);
            return v;
        }

        /// <summary>
        /// Parses a version number and sets all non-defined versions to 0, not -1.
        /// </summary>
        /// <param name="str"></param>
        /// <param name="version"></param>
        /// <returns></returns>
        private static bool TryParse(string str, out Version version)
        {
            if (Version.TryParse(str, out version))
            {
                // Fix values
                if (version.Build == -1) version = new Version(version.Major, version.Minor, 0, 0);
                else if (version.Revision == -1) version = new Version(version.Major, version.Minor, version.Build, 0);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Does a proper comparison check between versions, ignoring unset values as 0, not -1
        /// </summary>
        /// <param name="v1"></param>
        /// <param name="v2"></param>
        /// <returns></returns>
        public static int CompareVersions(Version v1, Version v2)
        {
            Version av1 = ParseProperVersion(v1.ToString());
            Version av2 = ParseProperVersion(v2.ToString());
            return av1.CompareTo(av2);
        }

        public static bool IsLessThan(Version v1, Version v2)
        {
            return CompareVersions(v1,v2) < 0;
        }

        public static bool IsGreaterThan(Version v1, Version v2)
        {
            return CompareVersions(v1, v2) > 0;
        }

        public static bool IsGreaterThanOrEqual(Version v1, Version v2)
        {
            return CompareVersions(v1, v2) >= 0;
        }
    }
}
