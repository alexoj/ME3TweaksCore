using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FontAwesome5;
using LegendaryExplorerCore.Gammtek.Extensions;
using LegendaryExplorerCore.Packages;
using ME3TweaksCore.Localization;
using ME3TweaksCore.Services;
using ME3TweaksCore.Services.Backup;
using ME3TweaksCore.Targets;
using ME3TweaksCoreWPF.Services.Backup;
using Serilog;

namespace ME3TweaksCoreWPF.Services
{
    /// <summary>
    /// Contains WPF-specific wrapper functions for VanillaDatabaseService.
    /// </summary>
    public static class VanillaDatabaseServiceWPF
    {

        /// <summary>
        /// Checks the existing listed backup and tags it with cmm_vanilla if determined to be vanilla. This is because ALOT Installer allows modified backups where as Mod Manager will not. WPF Extension: Icon
        /// </summary>
        /// <param name="game"></param>
        public static void CheckAndTagBackup(MEGame game)
        {
            BackupServiceWPF.SetIcon(game, EFontAwesomeIcon.Solid_Spinner);
            VanillaDatabaseService.CheckAndTagBackup(game);
            BackupServiceWPF.ResetIcon(game);
        }
    }
}
