using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using LegendaryExplorerCore.Packages;
using ME3TweaksCore.NativeMods;
using ME3TweaksCore.Targets;
using ME3TweaksCoreWPF.UI;

namespace ME3TweaksCoreWPF.NativeMods
{
    class ASIGameWPF : ASIGame
    {
        public ICommand InstallLoaderCommand { get; }

        public ASIGameWPF(MEGame game, List<GameTargetWPF> targets) : base(game, targets.OfType<GameTarget>().ToList())
        {
            InstallLoaderCommand = new GenericCommand(InstallLoader, CanInstallLoader);
        }
    }
}
