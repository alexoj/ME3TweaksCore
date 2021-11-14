using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using LegendaryExplorerCore.Packages;
using ME3TweaksCore.NativeMods;

namespace ME3TweaksCoreWPF.NativeMods
{
    public abstract class InstalledASIModWPF : InstalledASIMod
    {
        public abstract Brush BackgroundColor { get; }

        public InstalledASIModWPF(string asiFile, string hash, MEGame game) : base(asiFile, hash, game) { }
    }
}
