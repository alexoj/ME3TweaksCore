using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LegendaryExplorerCore.GameFilesystem;
using LegendaryExplorerCore.Helpers;
using LegendaryExplorerCore.Packages;
using ME3TweaksCore.GameFilesystem;
using ME3TweaksCore.Localization;
using ME3TweaksCore.Targets;

namespace ME3TweaksCore.Diagnostics
{
    /// <summary>
    ///  Contains various diagnostic utility methods
    /// </summary>
    public static class DiagnosticTools
    {

        /// <summary>
        /// Opens all used package files in the game and verifies they can be opened by LEC. This will catch things such as compression errors.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="progressCallback"></param>
        /// <returns></returns>
        public static List<string> VerifyPackages(GameTarget target, Action<int, int> progressCallback = null)
        {
            MLog.Information($@"Running diagnostic tool: VerifyPackages on {target.TargetPath}");
            List<string> errors = new List<string>();

            var loadedPackages = MELoadedFiles
                .GetFilesLoadedInGame(target.Game, true, gameRootOverride: target.TargetPath)
                .Where(x => x.Value.RepresentsPackageFilePath()).ToList();

            int done = 0;
            foreach (var packPath in loadedPackages)
            {
                try
                {
                    using var package = MEPackageHandler.OpenMEPackage(packPath.Value);
                }
                catch (Exception e)
                {
                    MLog.Exception(e, @"Error opening package file: ");
                    errors.Add(LC.GetString(LC.string_interp_failedToLoadPackageXY, packPath, e.FlattenException())); // Fat stack is probably more useful as it can trace where code failed.
                }

                done++;
                progressCallback?.Invoke(done, loadedPackages.Count);
            }

            return errors;
        }
    }
}