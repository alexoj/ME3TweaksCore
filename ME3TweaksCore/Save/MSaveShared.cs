using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using LegendaryExplorerCore.Packages;

namespace ME3TweaksCore.Save
{
    /// <summary>
    /// Save file related methods
    /// </summary>
    public class MSaveShared
    {
        /// <summary>
        /// Get time played in format of 1h 2m
        /// </summary>
        /// <param name="secondsPlayed"></param>
        /// <returns></returns>
        public static string GetTimePlayed(int secondsPlayed)
        {
            var hours = secondsPlayed / 3600;
            var minutes = secondsPlayed % 60;

            return $"{hours}h {minutes}m";
        }

        /// <summary>
        /// Converts difficulty setting to UI string
        /// </summary>
        /// <param name="difficulty"></param>
        /// <param name="game"></param>
        /// <returns></returns>
        public static string GetDifficultyString(int difficulty, MEGame game)
        {
            switch (difficulty)
            {
                case 0 when game.IsGame3():
                    return "Narrative";
                case 0:
                case 1 when game.IsGame3():
                    return "Casual";
                case 1:
                case 2 when game.IsGame3():
                    return "Normal";
                case 2:
                    return "Veteran";
                case 3:
                    return "Hardcore";
                case 4:
                    return "Insanity";
                case 5:
                    return "Debug difficulty"; // This is apparently a value in some instances.
                default:
                    return $"Unknown difficulty level: {difficulty}";
            }
        }
    }
}
