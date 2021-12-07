using System;
using LegendaryExplorerCore.Packages;

namespace ME3TweaksCore.Objects
{

    /// <summary>
    /// Pairs a localization with its human readable name.
    /// </summary>
    public class MELanguage
    {
        public MELanguage(MELocalization localization)
        {
            Localization = localization;
        }

        public MELocalization Localization { get; init; }

        public string HumanName
        {
            get
            {
                return Localization switch
                {
                    MELocalization.None => "None",
                    MELocalization.INT => "International English",
                    MELocalization.DEU => "German",
                    MELocalization.POL => "Polish",
                    MELocalization.ITA => "Italian",
                    MELocalization.RUS => "Russian",
                    MELocalization.ESN => "Spanish",
                    MELocalization.JPN => "Japanese",
                    MELocalization.FRA => "French",
                    _ => throw new ArgumentOutOfRangeException()
                };
            }
        }
    }
}
