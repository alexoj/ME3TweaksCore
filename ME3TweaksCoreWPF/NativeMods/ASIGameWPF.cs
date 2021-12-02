using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using LegendaryExplorerCore.Misc;
using LegendaryExplorerCore.Packages;
using ME3TweaksCore.NativeMods;
using ME3TweaksCore.Targets;
using ME3TweaksCoreWPF.Targets;
using ME3TweaksCoreWPF.UI;
using PropertyChanged;

namespace ME3TweaksCoreWPF.NativeMods
{
    /// <summary>
    /// WPF-extended version of ASIGame
    /// </summary>
    [AddINotifyPropertyChangedInterface]
    public class ASIGameWPF : ASIGame
    {
        public ICommand InstallLoaderCommand { get; }

        /// <summary>
        /// List of GameTargetWPFs that can be bound to in the UI. When this object is instantiated,
        /// both lists of targets is populated, so you can bind to either as necessary.
        /// </summary>
        public ObservableCollectionExtended<GameTargetWPF> GameTargetsWPF { get; } = new();
        /// <summary>
        /// Current selected WPF target
        /// </summary>
        public GameTargetWPF CurrentGameTargetWPF { get; set; }

        public ASIGameWPF(MEGame game, List<GameTargetWPF> targets) : base(game, targets.OfType<GameTarget>().ToList())
        {
            InstallLoaderCommand = new GenericCommand(InstallLoader, CanInstallLoader);
            GameTargetsWPF.ReplaceAll(targets);
        }

        /// <summary>
        /// Makes an ASI game for the specific target
        /// </summary>
        /// <param name="target"></param>
        public ASIGameWPF(GameTargetWPF target) : base(target)
        {
            InstallLoaderCommand = new GenericCommand(InstallLoader, CanInstallLoader);
            GameTargetsWPF.ReplaceAll(new[] { target });
        }

        //Do not delete - fody will link this
        public void OnCurrentGameTargetWPFChanged()
        {
            if (CurrentGameTarget != null)
            {
                RefreshBinkStatus();
                RefreshASIStates();
            }
        }
    }
}
