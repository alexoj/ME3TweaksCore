using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using LegendaryExplorerCore.Gammtek.Extensions;
using LegendaryExplorerCore.Packages;
using ME3TweaksCore.Diagnostics;
using PropertyChanged;

namespace ME3TweaksCore.Services.Backup
{
    [AddINotifyPropertyChangedInterface]
    public class GameBackupStatus
    {
        public string GameName => Game.ToGameName();
        /// <summary>
        /// Game this backup status if for
        /// </summary>
        public MEGame Game { get; internal set; }
        /// <summary>
        /// If a backup is available for use
        /// </summary>
        public bool BackedUp { get; internal set; }
        public bool BackupActivity { get; internal set; }
        public string BackupStatus { get; internal set; }
        public string BackupLocationStatus { get; internal set; }
        public string LinkActionText { get; internal set; }
        public string BackupActionText { get; internal set; }

        public void OnBackupStatusChanged()
        {
            Debug.WriteLine(BackupStatus);
        }

        internal GameBackupStatus(MEGame game)
        {
            Game = game;
        }

        /// <summary>
        /// Refreshes the backup status texts.
        /// </summary>
        /// <param name="installed">If the game is considered installed. If no backup is available, but game is 'installed', the text will show game not installed.</param>
        /// <param name="log">If the paths should be logged</param>
        internal void RefreshBackupStatus(bool installed, bool log)
        {
            var bPath = BackupService.GetGameBackupPath(Game);
            if (bPath != null)
            {
                // Backup is available
                BackupStatus = "Backed up";
                BackupLocationStatus = $"Backup stored at {bPath}";
                if (log) MLog.Information($@"BackupService: {Game} {BackupStatus}, {BackupLocationStatus}");
                LinkActionText = "Unlink backup";
                BackupActionText = "Restore game";
                BackedUp = true;
                return;
            }
            bPath = BackupService.GetGameBackupPath(Game, forceReturnPath: true);
            if (bPath == null)
            {
                // Backup path doesn't exist, no backup
                BackedUp = false;
                BackupStatus = "Not backed up";
                BackupLocationStatus = "Game has not been backed up";
                if (log) MLog.Information($@"BackupService: {Game} {BackupStatus}, {BackupLocationStatus}");
                LinkActionText = "Link existing backup";
                BackupActionText = "Create backup"; //This should be disabled if game is not installed. This will be handled by the wrapper
                return;
            }
            else if (!Directory.Exists(bPath))
            {
                // Backup path value DOES exist but the directory is not found
                BackedUp = false;
                BackupStatus = "Backup unavailable";
                BackupLocationStatus = $"Backup path not accessible: {bPath}";
                if (log) MLog.Information($@"BackupService: {Game} {BackupStatus}, {BackupLocationStatus}");
                LinkActionText = "Unlink backup";
                BackupActionText = "Create new backup";
                return;
            }

            if (!installed)
            {
                //BackedUp = false; // Not sure if this is the right call, maybe we shouldn't modify this
                BackupStatus = "Game not installed";
                BackupLocationStatus = "Game not installed. Run at least once to ensure game is fully setup";
                if (log) MLog.Information($@"BackupService: {Game} {BackupStatus}, {BackupLocationStatus}");
                LinkActionText = "Link existing backup"; //this seems dangerous to the average user
                BackupActionText = "Can't create backup";
            }
        }
    }
}
