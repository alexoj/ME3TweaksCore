using System.IO;
using System.Linq;
using System.Text;
using LegendaryExplorerCore.Gammtek.Collections.ObjectModel;
using LegendaryExplorerCore.Misc;
using ME3TweaksCore.Diagnostics;
using ME3TweaksCore.Helpers;
using ME3TweaksCore.Objects;

namespace ME3TweaksCore.GameFilesystem
{
    /// <summary>
    /// Class that represents data in _metacmm.txt files - files that describe the installed mod
    /// </summary>
    public class MetaCMM
    {
        #region Info Prefixes
        public static readonly string PrefixOptionsSelectedOnInstall = @"[INSTALLOPTIONS]";
        public static readonly string PrefixIncompatibleDLC = @"[INCOMPATIBLEDLC]";
        public static readonly string PrefixRequiredDLC = @"[REQUIREDDLC]";
        public static readonly string PrefixExtendedAttributes = @"[EXTENDEDATTRIBUTE]";
        public static readonly string PrefixModDescPath = @"[SOURCEMODDESC]";
        #endregion

        public string ModName { get; set; }
        public string Version { get; set; }
        public string InstalledBy { get; set; }
        public string InstallerInstanceGUID { get; set; }

        /// <summary>
        /// Path of moddesc.ini that was used to generate this (only used by M3)
        /// </summary>
        public string ModdescSourcePath { get; set; }
        /// <summary>
        /// List of DLC that this one is not compatible with
        /// </summary>
        public ObservableCollectionExtended<string> IncompatibleDLC { get; } = new ObservableCollectionExtended<string>();
        /// <summary>
        /// List of DLC that this one is not compatible with
        /// </summary>
        public ObservableCollectionExtended<DLCRequirement> RequiredDLC { get; } = new ObservableCollectionExtended<DLCRequirement>();
        /// <summary>
        /// List of selected install-time options
        /// </summary>
        public ObservableCollectionExtended<string> OptionsSelectedAtInstallTime { get; } = new ObservableCollectionExtended<string>();
        /// <summary>
        /// List of extended attributes that don't have a hardcoded variable in tooling.
        /// </summary>
        public CaseInsensitiveDictionary<string> ExtendedAttributes { get; } = new();

        /// <summary>
        /// Writes the metacmm file to the specified filepath.
        /// </summary>
        /// <param name="path">Filepath to write to</param>
        /// <param name="installingAppName">The managed installer name that is writing this metacmm file. An integer indicates a Mod Manager build number</param>
        public void WriteMetaCMM(string path, string installingAppName)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(ModName);
            sb.AppendLine(Version);
            sb.AppendLine(InstalledBy ?? installingAppName);
            sb.AppendLine(InstallerInstanceGUID);

            // MetaCMM Extended
            if (OptionsSelectedAtInstallTime.Any())
            {
                sb.AppendLine($@"{PrefixOptionsSelectedOnInstall}{string.Join(';', OptionsSelectedAtInstallTime)}");
            }
            if (IncompatibleDLC.Any())
            {
                sb.AppendLine($@"{PrefixIncompatibleDLC}{string.Join(';', IncompatibleDLC)}");
            }

            // Mod Manager 8: Write the source moddesc.ini path so we can potentially use it to track 
            // which library mod was used to install it in the event we ever track mods in the library
            if (ModdescSourcePath != null)
            {
                sb.AppendLine($@"{PrefixModDescPath}{ModdescSourcePath}");
            }

            if (RequiredDLC.Any())
            {
                sb.AppendLine($@"{PrefixRequiredDLC}{string.Join(';', RequiredDLC)}");
            }

            File.WriteAllText(path, sb.ToString());
        }

        public MetaCMM() { }

        /// <summary>
        /// Loads a metaCMM file from disk
        /// </summary>
        /// <param name="metaFile"></param>
        public MetaCMM(string metaFile)
        {
            var lines = MUtilities.WriteSafeReadAllLines(metaFile).ToList();
            int i = 0;
            foreach (var line in lines)
            {
                switch (i)
                {
                    case 0:
                        ModName = line;
                        break;
                    case 1:
                        Version = line;
                        break;
                    case 2:
                        InstalledBy = line;
                        break;
                    case 3:
                        InstallerInstanceGUID = line;
                        break;
                    default:
                        // MetaCMM Extended
                        if (line.StartsWith(PrefixOptionsSelectedOnInstall))
                        {
                            var parsedline = line.Substring(PrefixOptionsSelectedOnInstall.Length);
                            OptionsSelectedAtInstallTime.ReplaceAll(StringStructParser.GetSemicolonSplitList(parsedline));
                        }
                        else if (line.StartsWith(PrefixIncompatibleDLC))
                        {
                            var parsedline = line.Substring(PrefixIncompatibleDLC.Length);
                            IncompatibleDLC.ReplaceAll(StringStructParser.GetSemicolonSplitList(parsedline));
                        }
                        else if (line.StartsWith(PrefixExtendedAttributes))
                        {
                            var parsedline = line.Substring(PrefixExtendedAttributes.Length);
                            var entry = new DuplicatingIni.IniEntry(parsedline);
                            ExtendedAttributes.Add(entry.Key, entry.Value);
                        }
                        else if (line.StartsWith(PrefixModDescPath))
                        {
                            var parsedline = line.Substring(PrefixModDescPath.Length);
                            ModdescSourcePath = parsedline;
                        }
                        else if (line.StartsWith(PrefixRequiredDLC))
                        {
                            var parsedline = line.Substring(PrefixRequiredDLC.Length);
                            var split = parsedline.Split(';');
                            foreach (var s in split)
                            {
                                try
                                {
                                    RequiredDLC.Add(DLCRequirement.ParseRequirement(s, true));
                                }
                                catch
                                {
                                    MLog.Warning($@"Failed to read DLC requirement: {s} in metacmm file {metaFile}");
                                }
                            }
                        }
                        break;
                }
                i++;
            }
        }
    }
}
