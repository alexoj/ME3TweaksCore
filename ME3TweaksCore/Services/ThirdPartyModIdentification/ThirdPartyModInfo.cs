using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PropertyChanged;

namespace ME3TweaksCore.Services.ThirdPartyModIdentification
{
    [AddINotifyPropertyChangedInterface]
    public class ThirdPartyModInfo
    {
        public string dlcfoldername { get; set; } //This is also the key into the TPMIS dictionary. 
        public string modname { get; set; }
        public string moddev { get; set; }
        public string modsite { get; set; }
        public string moddesc { get; set; }
        public string mountpriority { get; set; }
        public string modulenumber { get; set; } // Game 2 only
        public string preventimport { get; set; }
        public string updatecode { get; set; } //has to be string I guess
        public int MountPriorityInt => string.IsNullOrWhiteSpace(mountpriority) ? 0 : int.Parse(mountpriority);

        /// <summary>
        /// Do not use this attribute, use IsOutdated instead.
        /// </summary>
        public string outdated { get; set; }

        public bool IsOutdated => string.IsNullOrWhiteSpace(outdated) || int.Parse(outdated) != 0;

        #region MOD MANAGER SPECIFIC

        /// <summary>
        /// Denotes that this TPMI object represents a preview object (such as in Starter Kit)
        /// </summary>
        public bool IsPreview { get; set; }

        /// <summary>
        /// Denotes this TPMI object is selected (UI only)
        /// </summary>
        public bool IsSelected { get; set; }

        public bool PreventImport => preventimport == "1" ? true : false;

        public string StarterKitString => $"{MountPriorityInt} - {modname}{(modulenumber != null ? " - Module # " + modulenumber : "")}"; //not worth localizing
        #endregion
    }
}
