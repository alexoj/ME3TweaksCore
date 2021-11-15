using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LegendaryExplorerCore.Packages;
using ME3TweaksCore.Services.ThirdPartyModIdentification;
using PropertyChanged;

namespace ME3TweaksCore.Targets
{
    [AddINotifyPropertyChangedInterface]
    public class InstalledOfficialDLC
    {
        public InstalledOfficialDLC(string foldername, bool installed, MEGame game)
        {
            FolderName = foldername;
            Installed = installed;
            HumanName = TPMIService.GetThirdPartyModInfo(FolderName, game)?.modname ?? foldername;
        }

        public string FolderName { get; set; }
        public bool Installed { get; set; }
        public string HumanName { get; set; }
    }
}
