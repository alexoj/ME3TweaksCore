using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ME3TweaksCore.Helpers;

namespace ME3TweaksCore.Objects
{
    public class DLCOptionKey
    {
        public const string KEY_OPTIONKEY = @"option";
        public const string KEY_UISTRING = @"uistring";

        public string UIString { get; set; }
        public PlusMinusKey OptionKey { get; set; }

        public DLCOptionKey(string inputString, double featureLevel)
        {
            // Feature level is just moddesc version. The initial version supported is 9, so only check for higher values.
            var map = StringStructParser.GetSplitMapValues(inputString, true, '[', ']');
            if (map.TryGetValue(KEY_OPTIONKEY, out var optionKey))
            {
                OptionKey = new PlusMinusKey(optionKey);
            }
            else
            {
                throw new Exception($"DLCOptionKey structs must contain a descriptor named '{KEY_OPTIONKEY}'");
            }

            if (map.TryGetValue(KEY_UISTRING, out var uiStr))
            {
                UIString = uiStr;
            }
        }

        public string ToUIString()
        {
            if (OptionKey.IsPlus == true)
                return $"(with option {UIString ?? OptionKey.Key} installed)";
            if (OptionKey.IsPlus == false)
                return $"(with option {UIString ?? OptionKey.Key} NOT installed)";
            return "";
        }
    }
}
