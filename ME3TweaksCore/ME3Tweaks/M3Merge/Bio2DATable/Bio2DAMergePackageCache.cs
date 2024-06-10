using System.Collections.Generic;
using System.Linq;
using LegendaryExplorerCore.Helpers;
using LegendaryExplorerCore.Misc;
using LegendaryExplorerCore.Packages;

namespace ME3TweaksCore.ME3Tweaks.M3Merge.Bio2DATable
{
    /// <summary>
    /// Class that holds references to packages for Bio2DA merge
    /// </summary>
    internal class Bio2DAMergePackageContainer
    {
        private readonly List<IMEPackage> TargetPackages = new List<IMEPackage>();
        private readonly List<IMEPackage> ModPackages = new List<IMEPackage>();
        public readonly CaseInsensitiveDictionary<string> OriginalHashes = new();

        public void InsertTargetPackage(IMEPackage targetPackage)
        {
            TargetPackages.Add(targetPackage);
        }

        public void InsertModPackage(IMEPackage modPackage)
        {
            ModPackages.Add(modPackage);
        }

        public IEnumerable<IMEPackage> GetTargetablePackages()
        {
            return TargetPackages;
        }

        public IEnumerable<IMEPackage> GetModPackages()
        {
            return ModPackages;
        }

        /// <summary>
        /// Returns a target package based on filepath
        /// </summary>
        /// <param name="packagePath"></param>
        /// <returns></returns>
        public IMEPackage GetTargetPackage(string packagePath)
        {
            return TargetPackages.FirstOrDefault(x=> packagePath.CaseInsensitiveEquals(x.FilePath));
        }

        /// <summary>
        /// Returns a mod package based on filepath
        /// </summary>
        /// <param name="packagePath"></param>
        /// <returns></returns>
        public IMEPackage GetModPackage(string packagePath)
        {
            return ModPackages.FirstOrDefault(x => packagePath.CaseInsensitiveEquals(x.FilePath));
        }
    }
}
