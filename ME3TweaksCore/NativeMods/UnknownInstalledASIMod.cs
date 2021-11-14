using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LegendaryExplorerCore.Packages;

namespace ME3TweaksCore.NativeMods
{
    /// <summary>
    /// ASI mod that is not in the manifest
    /// </summary>
    public class UnknownInstalledASIMod : InstalledASIMod
    {
        public UnknownInstalledASIMod(string filepath, string hash, MEGame game) : base(filepath, hash, game)
        {
            UnmappedFilename = Path.GetFileNameWithoutExtension(filepath);
        }
        public string UnmappedFilename { get; set; }
    }
}
