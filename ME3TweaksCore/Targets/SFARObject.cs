using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using LegendaryExplorerCore.GameFilesystem;
using ME3TweaksCore.Helpers;
using ME3TweaksCore.Services;
using PropertyChanged;
using Serilog;

namespace ME3TweaksCore.Targets
{
    [AddINotifyPropertyChangedInterface]
    public class SFARObject
    {
        public SFARObject(string file, GameTarget target, Func<string, bool> restoreSFARCallback,
            Action startingRestoreCallback, Action<object> notifyNoLongerModifiedCallback)
        {
            RestoreConfirmationCallback = restoreSFARCallback;
            IsModified = true;
            this.startingRestoreCallback = startingRestoreCallback;
            this.notifyNoLongerModified = notifyNoLongerModifiedCallback;
            this.target = target;
            Unpacked = new FileInfo(file).Length == 32;
            DLCDirectory = Directory.GetParent(Directory.GetParent(file).FullName).FullName;
            FilePath = file.Substring(target.TargetPath.Length + 1);
            if (Path.GetFileName(file) == @"Patch_001.sfar")
            {
                UIString = @"TestPatch";
                IsMPSFAR = true;
                IsSPSFAR = true;
            }
            else
            {
                var dlcFoldername = Directory.GetParent(Directory.GetParent(file).FullName).FullName;
                if (dlcFoldername.Contains(@"DLC_UPD") || dlcFoldername.Contains(@"DLC_CON_MP"))
                {
                    IsMPSFAR = true;
                }
                else
                {
                    IsSPSFAR = true;
                }

                ME3Directory.OfficialDLCNames.TryGetValue(Path.GetFileName(dlcFoldername), out var name);
                UIString = name;
                if (Unpacked)
                {
                    UIString += @" - " + LC.GetString(LC.string_unpacked);
                }

                if (!Unpacked)
                {
                    var filesInSfarDir = Directory.EnumerateFiles(DLCDirectory, @"*.*", SearchOption.AllDirectories).ToList();
                    if (filesInSfarDir.Any(d =>
                        !Path.GetFileName(d).Equals(@"PCConsoleTOC.bin", StringComparison.InvariantCultureIgnoreCase) && //pcconsoletoc will be produced for all folders even with autotoc asi even if its not needed
                        VanillaDatabaseService.UnpackedFileExtensions.Contains(Path.GetExtension(d.ToLower()))))
                    {
                        Inconsistent = true;
                    }
                }
            }
        }

        public bool IsSPSFAR { get; private set; }
        public bool IsMPSFAR { get; private set; }

        public bool RevalidateIsModified(bool notify = true)
        {
            bool _isModified = IsModified;
            IsModified = !VanillaDatabaseService.IsFileVanilla(target, Path.Combine(target.TargetPath, FilePath));
            if (!IsModified && _isModified && notify)
            {
                //Debug.WriteLine("Notifying that " + FilePath + " is no longer modified.");
                notifyNoLongerModified?.Invoke(this);
            }

            return IsModified;
        }

        public bool IsModified { get; set; }

        private Action<object> notifyNoLongerModified;

        protected void RestoreSFARWrapper()
        {
            RestoreSFAR(false);
        }

        public void RestoreSFAR(bool batchRestore, Action signalRestoreCompleted = null)
        {
            bool? restore = batchRestore;

            if (!restore.Value) restore = RestoreConfirmationCallback?.Invoke(FilePath);
            if (restore.HasValue && restore.Value)
            {
                NamedBackgroundWorker nbw = new NamedBackgroundWorker(@"RestoreSFARThread");
                nbw.DoWork += (a, b) =>
                {
                    var bup = BackupService.GetGameBackupPath(target.Game);
                    if (bup != null)
                    {
                        var backupFile = Path.Combine(bup, FilePath);
                        var targetFile = Path.Combine(target.TargetPath, FilePath);
                        Restoring = true;

                        var unpackedFiles = Directory.GetFiles(DLCDirectory, @"*", SearchOption.AllDirectories);
                        RestoreButtonContent = LC.GetString(LC.string_cleaningUp);
                        foreach (var file in unpackedFiles)
                        {
                            if (!file.EndsWith(@".sfar"))
                            {
                                Log.Information(@"Deleting unpacked file: " + file);
                                File.Delete(file);
                            }
                        }

                        // Check if we actually need to restore SFAR
                        if (new FileInfo(targetFile).Length == 32 || !VanillaDatabaseService.IsFileVanilla(target, targetFile, false))
                        {
                            Log.Information($@"Restoring SFAR from backup: {backupFile} -> {targetFile}");
                            XCopy.Copy(backupFile, targetFile, true, true,
                                (o, pce) =>
                                {
                                    RestoreButtonContent = LC.GetString(LC.string_interp_restoringXpercent,
                                        pce.ProgressPercentage.ToString());
                                });
                        }

                        MUtilities.DeleteEmptySubdirectories(DLCDirectory);
                        RestoreButtonContent = LC.GetString(LC.string_restored);
                    }
                    else
                    {
                        Restoring = false;
                    }
                };
                nbw.RunWorkerCompleted += (a, b) =>
                {
                    if (b.Error != null)
                    {
                        Log.Error($@"Exception occurred in {nbw.Name} thread: {b.Error.Message}");
                    }
                    //File.Copy(backupFile, targetFile, true);
                    //if (!batchRestore)
                    //{
                    RevalidateIsModified();
                    //restoreCompletedCallback?.Invoke();
                    //}
                    Restoring = false;
                    signalRestoreCompleted?.Invoke();
                };
                startingRestoreCallback?.Invoke();
                nbw.RunWorkerAsync();
            }
        }


        public static bool HasUnpackedFiles(string sfarFile)
        {
            var unpackedFiles =
                Directory.GetFiles(Directory.GetParent(Directory.GetParent(sfarFile).FullName).FullName, @"*",
                    SearchOption.AllDirectories);
            return (unpackedFiles.Any(x =>
                Path.GetExtension(x) == @".bin" && Path.GetFileNameWithoutExtension(x) != @"PCConsoleTOC"));
        }

        private bool checkedForBackupFile;
        private bool canRestoreSfar;
        public bool Restoring { get; set; }
        public bool OtherSFARBeingRestored { get; set; }

        protected bool CanRestoreSFAR()
        {
            if (Restoring) return false;
            if (OtherSFARBeingRestored) return false;
            if (checkedForBackupFile) return canRestoreSfar;
            var backupPath = BackupService.GetGameBackupPath(target.Game);
            canRestoreSfar = backupPath != null && File.Exists(Path.Combine(backupPath, FilePath));
            checkedForBackupFile = true;
            if (!canRestoreSfar)
            {
                RestoreButtonContent = LC.GetString(LC.string_noBackup);
            }
            return canRestoreSfar;
        }

        private Func<string, bool> RestoreConfirmationCallback;

        public string RestoreButtonContent { get; set; } = LC.GetString(LC.string_restore);

        private Action startingRestoreCallback;
        private readonly GameTarget target;
        private readonly bool Unpacked;

        public string DLCDirectory { get; }

        public string FilePath { get; }
        public string UIString { get; }
        public bool Inconsistent { get; }

    }

}
