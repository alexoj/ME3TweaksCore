using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using LegendaryExplorerCore.Packages;

namespace ME3TweaksCoreWPF.NativeMods
{
    public class UnknownInstalledASIModWPF : InstalledASIModWPF
    {
        private static Brush brush = new SolidColorBrush(Color.FromArgb(0x88, 0xFF, 0x10, 0x10));
        public override Brush BackgroundColor => brush;

        public UnknownInstalledASIModWPF(string filepath, string hash, MEGame game) : base(filepath, hash, game){ }
    }
}
