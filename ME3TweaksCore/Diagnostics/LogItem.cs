using System.IO;
using LegendaryExplorerCore.Helpers;
using ME3TweaksCore.Localization;

namespace ME3TweaksCore.Diagnostics
{
    public class LogItem
    {
        /// <summary>
        /// If this LogItem can be uploaded (represents a real path)
        /// </summary>
        public bool Selectable { get; set; } = true;

        public string filepath;
        public LogItem(string filepath)
        {
            this.filepath = filepath;
        }
        /// <summary>
        /// If this is the current session log
        /// </summary>
        public bool IsActiveLog { get; set; }
        public override string ToString()
        {
            if (!Selectable)
                return filepath; // Do nothing on this.
            if (IsActiveLog)
                return LC.GetString(LC.string_interp_currentLog, Path.GetFileName(filepath), FileSize.FormatSize(new FileInfo(filepath).Length));
            return $@"{Path.GetFileName(filepath)} - {FileSize.FormatSize(new FileInfo(filepath).Length)}";
        }
    }
}