using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LegendaryExplorerCore.GameFilesystem;
using LegendaryExplorerCore.Gammtek.Extensions;
using LegendaryExplorerCore.Misc;
using LegendaryExplorerCore.Packages;
using ME3TweaksCore.Diagnostics;
using ME3TweaksCore.GameFilesystem;
using ME3TweaksCore.Helpers;
using ME3TweaksCore.Localization;
using ME3TweaksCore.Misc;
using ME3TweaksCore.Services.Backup;
using ME3TweaksCore.Targets;
using PropertyChanged;
using RoboSharp;
using Serilog;

namespace ME3TweaksCore.Services.Restore
{
    #region RESTORE

    public class GameRestore : INotifyPropertyChanged
    {
        private MEGame Game;

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Callback for when there is a blocking error and the restore cannot be performed
        /// </summary>
        public Action<string, string> BlockingErrorCallback { get; set; }
        /// <summary>
        /// Callback for when there is an error during the restore. This may mean you need to keep UI target available still so user can try again without losing the target
        /// </summary>
        public Action<string, string> RestoreErrorCallback { get; set; }
        /// <summary>
        /// Callback to select a directory for custom restore location
        /// </summary>
        public Func<string, string, string> SelectDestinationDirectoryCallback { get; set; }
        /// <summary>
        /// Callback to confirm restoration over existing game
        /// </summary>
        public Func<string, string, bool> ConfirmationCallback { get; set; }
        /// <summary>
        /// Callback when the status string on the UI should be updated
        /// </summary>
        public Action<string> UpdateStatusCallback { get; set; }
        /// <summary>
        /// Callback when there is a progress update for the UI
        /// </summary>
        public Action<long, long> UpdateProgressCallback { get; set; }
        /// <summary>
        /// Callback when the progressbar should change indeterminate states
        /// </summary>
        public Action<bool> SetProgressIndeterminateCallback { get; set; }

        public GameRestore(MEGame game)
        {
            this.Game = game;
        }

        /// <summary>
        /// Restores the game to the specified directory (game location). Pass in null if you wish to restore to a custom location.
        /// </summary>
        /// <param name="destinationDirectory">Game directory that will be replaced with backup</param>
        /// <returns></returns>
        public bool PerformRestore(GameTarget restoreTarget, string destinationDirectory)
        {
            int ProgressValue = 0;
            int ProgressMax = 0;
            if (MUtilities.IsGameRunning(Game))
            {
                BlockingErrorCallback?.Invoke("Cannot restore game", $"Cannot restore {Game.ToGameName()} while an instance of it is running.");
                return false;
            }

            bool restore = destinationDirectory == null; // Restore to custom location
            if (!restore)
            {
                var confirmDeletion = ConfirmationCallback?.Invoke($"Restoring {Game.ToGameName()} will delete existing installation", $"Restoring {Game.ToGameName()} will delete the existing installation, copy your backup to its original location, and reset the texture Level of Detail (LOD) settings for your game.");
                restore |= confirmDeletion.HasValue && confirmDeletion.Value;
            }

            if (restore)
            {
                string backupPath = BackupService.GetGameBackupPath(Game);

                if (destinationDirectory == null)
                {
                    destinationDirectory = SelectDestinationDirectoryCallback?.Invoke("Select destination location for restore", "Select a directory to restore the game to. This directory must be empty.");
                    if (destinationDirectory != null)
                    {
                        //Check empty
                        if (Directory.Exists(destinationDirectory))
                        {
                            if (Directory.GetFiles(destinationDirectory).Length > 0 || Directory.GetDirectories(destinationDirectory).Length > 0)
                            {
                                //Directory not empty
                                BlockingErrorCallback?.Invoke("Cannot restore game", "The destination directory for restores must be an empty directory. Remove the files and folders in this directory, or choose another directory.");
                                return false;
                            }

                            //TODO: PREVENT RESTORING TO DOCUMENTS/BIOWARE

                        }

                        TelemetryInterposer.TrackEvent(@"Chose to restore game to custom location", new Dictionary<string, string>() { { @"Game", Game.ToString() } });

                    }
                    else
                    {
                        Log.Warning("User declined to choose destination directory");
                        return false;
                    }
                }

                SetProgressIndeterminateCallback?.Invoke(true);
                UpdateStatusCallback?.Invoke("Deleting existing game installation");
                if (Directory.Exists(destinationDirectory))
                {
                    if (Enumerable.Any(Directory.GetFiles(destinationDirectory)) || Enumerable.Any(Directory.GetDirectories(destinationDirectory)))
                    {
                        Log.Information(@"Deleting existing game directory: " + destinationDirectory);
                        try
                        {
                            bool deletedDirectory = MUtilities.DeleteFilesAndFoldersRecursively(destinationDirectory);
                            if (deletedDirectory != true)
                            {
                                RestoreErrorCallback?.Invoke("Could not delete game directory", $"Could not delete the game directory for {Game.ToGameName()}. The game will be in a semi deleted state, please manually delete it and then restore to the same location as the game to fully restore the game.");
                                //b.Result = RestoreResult.ERROR_COULD_NOT_DELETE_GAME_DIRECTORY;
                                return false;
                            }
                        }
                        catch (Exception ex)
                        {
                            //todo: handle this better
                            Log.Error($@"Exception deleting game directory: {destinationDirectory}: {ex.Message}");
                            RestoreErrorCallback?.Invoke("Error deleting game directory", $"Could not delete the game directory for {Game.ToGameName()}: {ex.Message}. The game will be in a semi deleted state, please manually delete it and then restore to the same location as the game to fully restore the game.");
                            //b.Result = RestoreResult.EXCEPTION_DELETING_GAME_DIRECTORY;
                            return false;
                        }
                    }
                }
                else
                {
                    Log.Error(@"Game directory not found! Was it removed while the app was running?");
                }

                var created = MUtilities.CreateDirectoryWithWritePermission(destinationDirectory);
                if (!created)
                {
                    RestoreErrorCallback?.Invoke("Error creating game directory", $"Could not create the game directory for {Game.ToGameName()}. You may not have permissions to create folders in the directory that contains the game directory.");
                    //b.Result = RestoreResult.ERROR_COULD_NOT_CREATE_DIRECTORY;
                    return false;
                }

                UpdateStatusCallback?.Invoke("Restoring game from backup");
                //callbacks

                #region callbacks

                void fileCopiedCallback()
                {
                    ProgressValue++;
                    if (ProgressMax != 0)
                    {
                        UpdateProgressCallback?.Invoke(ProgressValue, ProgressMax);
                    }
                }

                string dlcFolderpath = MEDirectories.GetDLCPath(Game, backupPath) + Path.DirectorySeparatorChar; //\ at end makes sure we are restoring a subdir
                int dlcSubStringLen = dlcFolderpath.Length;
                //Debug.WriteLine(@"DLC Folder: " + dlcFolderpath);
                //Debug.Write(@"DLC Folder path len:" + dlcFolderpath);

                // Cached stuff to avoid hitting same codepath thousands of times
                var officialDLCNames = MEDirectories.OfficialDLCNames(Game);

                bool aboutToCopyCallback(string fileBeingCopied)
                {
                    if (fileBeingCopied.Contains(@"\cmmbackup\")) return false; //do not copy cmmbackup files
                    Debug.WriteLine(fileBeingCopied);
                    if (fileBeingCopied.StartsWith(dlcFolderpath, StringComparison.InvariantCultureIgnoreCase))
                    {
                        //It's a DLC!
                        string dlcname = fileBeingCopied.Substring(dlcSubStringLen);
                        int index = dlcname.IndexOf(Path.DirectorySeparatorChar);
                        if (index > 0) //Files directly in the DLC directory won't have path sep
                        {
                            try
                            {
                                dlcname = dlcname.Substring(0, index);
                                if (officialDLCNames.TryGetValue(dlcname, out var hrName))
                                {
                                    UpdateStatusCallback?.Invoke($"Restoring {hrName}");
                                }
                                else
                                {
                                    UpdateStatusCallback?.Invoke($"Restoring {dlcname}");
                                }
                            }
                            catch (Exception e)
                            {
                                TelemetryInterposer.TrackError(e, new Dictionary<string, string>()
                                    {
                                        {@"Source", @"Restore UI display callback"},
                                        {@"Value", fileBeingCopied},
                                        {@"DLC Folder path", dlcFolderpath}
                                    });
                            }
                        }
                    }
                    else
                    {
                        //It's basegame
                        if (fileBeingCopied.EndsWith(@".bik"))
                        {
                            UpdateStatusCallback?.Invoke("Restoring movies");
                        }
                        else if (new FileInfo(fileBeingCopied).Length > 52428800)
                        {
                            UpdateStatusCallback?.Invoke($"Restoring {Path.GetFileName(fileBeingCopied)}");
                        }
                        else
                        {
                            UpdateStatusCallback?.Invoke($"Restoring basegame");
                        }
                    }

                    return true;
                }

                void totalFilesToCopyCallback(int total)
                {
                    ProgressValue = 0;
                    SetProgressIndeterminateCallback?.Invoke(false);
                    ProgressMax = total;
                }

                void bigFileProgressCallback(string fileBeingCopied, long dataCopied, long totalDataToCopy)
                {
                    if (fileBeingCopied.StartsWith(dlcFolderpath, StringComparison.InvariantCultureIgnoreCase))
                    {
                        //It's a DLC!
                        string dlcname = fileBeingCopied.Substring(dlcSubStringLen);
                        int index = dlcname.IndexOf(Path.DirectorySeparatorChar);
                        try
                        {
                            string prefix = "Restoring ";
                            dlcname = dlcname.Substring(0, index);
                            if (officialDLCNames.TryGetValue(dlcname, out var hrName))
                            {
                                prefix += hrName;
                            }
                            else
                            {
                                prefix += dlcname;
                            }

                            UpdateStatusCallback?.Invoke($"{prefix} {(int)(dataCopied * 100d / totalDataToCopy)}%");
                        }
                        catch
                        {

                        }
                    }
                    else
                    {
                        UpdateStatusCallback?.Invoke($"Restoring {Path.GetFileName(fileBeingCopied)} {(int)(dataCopied * 100d / totalDataToCopy)}%");
                    }
                }

                #endregion

                UpdateStatusCallback?.Invoke("Calculating how many files will be restored");
                Log.Information($@"Copying backup to game directory: {backupPath} -> {destinationDirectory}");
                CopyTools.CopyAll_ProgressBar(new DirectoryInfo(backupPath), new DirectoryInfo(destinationDirectory),
                    totalItemsToCopyCallback: totalFilesToCopyCallback,
                    aboutToCopyCallback: aboutToCopyCallback,
                    fileCopiedCallback: fileCopiedCallback,
                    ignoredExtensions: new[] { @"*.pdf", @"*.mp3", @"*.bak" },
                    bigFileProgressCallback: bigFileProgressCallback);
                Log.Information(@"Restore of game data has completed");

                //Check for cmmvanilla file and remove it present

                string cmmVanilla = Path.Combine(destinationDirectory, @"cmm_vanilla");
                if (File.Exists(cmmVanilla))
                {
                    Log.Information(@"Removing cmm_vanilla file");
                    File.Delete(cmmVanilla);
                }

                Log.Information(@"Restore thread wrapping up");
                restoreTarget?.ReloadGameTarget(); // Reload target if we were passed in one.
                return true;
            }

            return false;
        }
    }

    #endregion
}