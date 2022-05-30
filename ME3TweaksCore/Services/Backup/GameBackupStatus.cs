using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using LegendaryExplorerCore.Gammtek.Extensions;
using LegendaryExplorerCore.Packages;
using ME3TweaksCore.Diagnostics;
using ME3TweaksCore.Localization;
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

        /// <summary>
        /// Location where the backup is MARKED to be. This doesn't mean it exists there, only that this is the value read from the settings! This is used to determine if a backup can be unlinked!
        /// </summary>
        public string MarkedBackupLocation { get; internal set; }

        public event EventHandler OnBackupStateChanged;

        /// <summary>
        /// Called when the backed up state changes
        /// </summary>
        private void OnBackedUpChanged()
        {
            OnBackupStateChanged?.Invoke(this, EventArgs.Empty);
            //Debug.WriteLine(BackupStatus);
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
                BackupStatus = LC.GetString(LC.string_backedUp);
                BackupLocationStatus = LC.GetString(LC.string_interp_backupStoredAtX, bPath);
                if (log) MLog.Information($@"BackupService: {Game} {BackupStatus}, {BackupLocationStatus}");
                LinkActionText = LC.GetString(LC.string_unlinkBackup);
                BackupActionText = LC.GetString(LC.string_restoreGame);
                BackedUp = true;
                MarkedBackupLocation = bPath;
                return;
            }
            bPath = BackupService.GetGameBackupPath(Game, forceReturnPath: true);
            MarkedBackupLocation = bPath;
            if (bPath == null)
            {
                // Backup path doesn't exist, no backup
                BackedUp = false;
                BackupStatus = LC.GetString(LC.string_notBackedUp);
                BackupLocationStatus = LC.GetString(LC.string_gameHasNotBeenBackedUp);
                if (log) MLog.Information($@"BackupService: {Game} {BackupStatus}, {BackupLocationStatus}");
                LinkActionText = LC.GetString(LC.string_linkExistingBackup);
                BackupActionText = LC.GetString(LC.string_createBackup); //This should be disabled if game is not installed. This will be handled by the wrapper
                return;
            }
            else if (!Directory.Exists(bPath))
            {
                // Backup path value DOES exist but the directory is not found
                BackedUp = false;
                BackupStatus = LC.GetString(LC.string_backupUnavailable);
                BackupLocationStatus = LC.GetString(LC.string_interp_backupPathNotAccessibleX, bPath);
                if (log) MLog.Information($@"BackupService: {Game} {BackupStatus}, {BackupLocationStatus}");
                LinkActionText = LC.GetString(LC.string_unlinkBackup);
                BackupActionText = LC.GetString(LC.string_createNewBackup);
                return;
            }

            if (!installed)
            {
                //BackedUp = false; // Not sure if this is the right call, maybe we shouldn't modify this
                BackupStatus = LC.GetString(LC.string_gameNotInstalled);
                BackupLocationStatus = LC.GetString(LC.string_gameNotInstalledRunOnce);
                if (log) MLog.Information($@"BackupService: {Game} {BackupStatus}, {BackupLocationStatus}");
                LinkActionText = LC.GetString(LC.string_linkExistingBackup); //this seems dangerous to the average user
                BackupActionText = LC.GetString(LC.string_cantCreateBackup);
            }
        }
    }
}
