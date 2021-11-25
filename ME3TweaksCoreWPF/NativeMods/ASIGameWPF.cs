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
    /// <summary>
    /// WPF-extended version of ASIGame
    /// </summary>
    public class ASIGameWPF : ASIGame
    {
        public ICommand InstallLoaderCommand { get; }

        /// <summary>
        /// The currently selected target (UI bound)
        /// </summary>
        public GameTarget SelectedTarget { get; set; }

        public ASIGameWPF(MEGame game, List<GameTargetWPF> targets) : base(game, targets.OfType<GameTarget>().ToList())
        {
            InstallLoaderCommand = new GenericCommand(InstallLoader, CanInstallLoader);
        }

        /// <summary>
        /// Makes an ASI game for the specific target
        /// </summary>
        /// <param name="target"></param>
        public ASIGameWPF(GameTargetWPF target) : base(target)
        {
            InstallLoaderCommand = new GenericCommand(InstallLoader, CanInstallLoader);
        }
    }
}
