using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using LegendaryExplorerCore.Packages;
using ME3TweaksCore.Localization;

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

            return LC.GetString(LC.string_interp_XhoursYMinutes, hours, minutes);
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
                    return LC.GetString(LC.string_narrative);
                case 0:
                case 1 when game.IsGame3():
                    return LC.GetString(LC.string_casual);
                case 1:
                case 2 when game.IsGame3():
                    return LC.GetString(LC.string_normal);
                case 2:
                    return LC.GetString(LC.string_veteran);
                case 3:
                    return LC.GetString(LC.string_hardcore);
                case 4:
                    return LC.GetString(LC.string_insanity);
                case 5:
                    return LC.GetString(LC.string_debugDifficulty); // This is apparently a value in some instances.
                default:
                    return LC.GetString(LC.string_interp_unknownDifficultyLevelX, difficulty);
            }
        }
    }
}
