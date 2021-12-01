using System.IO;
using LegendaryExplorerCore.Helpers;

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

        public override string ToString()
        {
            if (!Selectable)
                return filepath; // Do nothing on this.
            return $"{Path.GetFileName(filepath)} - {FileSize.FormatSize(new FileInfo(filepath).Length)}";
        }
    }
}
