using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ME3TweaksCore.Helpers;
using ME3TweaksCore.Localization;

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
                throw new Exception(LC.GetString(LC.string_interp_dLCOptionKeyStructsMustContainADescriptorNamedKEY_OPTIONKEY, KEY_OPTIONKEY));
            }

            if (map.TryGetValue(KEY_UISTRING, out var uiStr))
            {
                UIString = uiStr;
            }
        }

        public override string ToString()
        {
            Dictionary<string, string> dict = new Dictionary<string, string>()
            {
                { KEY_OPTIONKEY, OptionKey.ToString()}
            };

            if (!string.IsNullOrWhiteSpace(UIString))
            {
                dict[KEY_UISTRING] = UIString;
            }

            return StringStructParser.BuildSeparatedSplitValueList(dict, ',', '[', ']', quoteValues: false);
        }

        public string ToUIString()
        {
            if (OptionKey.IsPlus == true)
                return LC.GetString(LC.string_interp_dok_withOptionXInstalled, UIString ?? OptionKey.Key);
            if (OptionKey.IsPlus == false)
                return LC.GetString(LC.string_interp_dok_withOptionXNotInstalled, UIString ?? OptionKey.Key);
            return "";
        }
    }
}
