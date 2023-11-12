using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using ME3TweaksCore.Diagnostics;
using ME3TweaksCore.Helpers;
using ME3TweaksCore.Localization;
using ME3TweaksCore.Services.Backup;
using ME3TweaksCore.Services.Shared.BasegameFileIdentification;
using PropertyChanged;
using Serilog;

namespace ME3TweaksCore.Targets
{
    [AddINotifyPropertyChangedInterface]
    public class ModifiedFileObject
    {
        private bool canRestoreFile;
        private bool checkedForBackupFile;
        public string FilePath { get; }
        private GameTarget target;
        private Action<object> notifyRestoredCallback;
        private Action notifyRestoringCallback;
        private Func<string, bool> restoreBasegamefileConfirmationCallback;
        public bool Restoring { get; set; }

        public ModifiedFileObject(string filePath, GameTarget target,
            Func<string, bool> restoreBasegamefileConfirmationCallback,
            Action notifyRestoringFileCallback,
            Action<object> notifyRestoredCallback)
        {
            this.FilePath = filePath;
            this.target = target;
            this.notifyRestoredCallback = notifyRestoredCallback;
            this.restoreBasegamefileConfirmationCallback = restoreBasegamefileConfirmationCallback;
            this.notifyRestoringCallback = notifyRestoringFileCallback;
        }

        public string ModificationSource { get; set; }

        /// <summary>
        /// Uses the basegame file DB to attempt to determine the source mod of this file. This method should be run on a background thread.
        /// </summary>
        public void DetermineSource()
        {
            var fullpath = Path.Combine(target.TargetPath, FilePath);
            if (File.Exists(fullpath))
            {
                //if (FilePath.Equals(Utilities.Getth, StringComparison.InvariantCultureIgnoreCase)) return; //don't report this file
                var info = BasegameFileIdentificationService.GetBasegameFileSource(target, fullpath);
                if (info != null)
                {
                    ModificationSource = info.source;
                }
                // TODO: MAKE LOCAL DB??
#if DEBUG
                else
                {
                    ModificationSource = MUtilities.CalculateHash(fullpath);
                }
#endif
            }
        }

        public void RestoreFileWrapper()
        {
            RestoreFile(false);
        }

        public void RestoreFile(bool batchRestore)
        {
            bool? restore = batchRestore;
            if (!restore.Value) restore = restoreBasegamefileConfirmationCallback?.Invoke(FilePath);
            if (restore.HasValue && restore.Value && internalCanRestoreFile(batchRestore))
            {
                //Todo: Background thread this maybe?
                var backupPath = BackupService.GetGameBackupPath(target.Game);
                var backupFile = Path.Combine(backupPath, FilePath);
                var targetFile = Path.Combine(target.TargetPath, FilePath);
                try
                {
                    Restoring = true;
                    MLog.Information(@"Restoring basegame file: " + targetFile);
                    notifyRestoringCallback?.Invoke();
                    var tfi = new FileInfo(targetFile);
                    if (tfi.IsReadOnly)
                    {
                        tfi.IsReadOnly = false;
                    }
                    File.Copy(backupFile, targetFile, true);
                    notifyRestoredCallback?.Invoke(this);
                }
                catch (Exception e)
                {
                    Restoring = false;
                    notifyRestoredCallback?.Invoke(this);
                    MLog.Error($@"Error restoring file {targetFile}: " + e.Message);
                }
            }
        }

        //might need to make this more efficient...
        public string RestoreButtonText => Restoring ? LC.GetString(LC.string_restoring) : (CanRestoreFile() ? LC.GetString(LC.string_restore) : LC.GetString(LC.string_noBackup));

        public bool CanRestoreFile()
        {
            return internalCanRestoreFile(false);
        }

        private bool internalCanRestoreFile(bool batchMode)
        {
            if (Restoring && !batchMode) return false;
            if (checkedForBackupFile) return canRestoreFile;
            var backupPath = BackupService.GetGameBackupPath(target.Game);
            canRestoreFile = backupPath != null && File.Exists(Path.Combine(backupPath, FilePath));
            checkedForBackupFile = true;
            return canRestoreFile;
        }
    }
}
