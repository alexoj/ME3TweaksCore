using System.IO;
using LegendaryExplorerCore.Helpers;

namespace ME3TweaksCore.Diagnostics
{
    public class LogItem
    {
        public string filepath;
        public LogItem(string filepath)
        {
            this.filepath = filepath;
        }

        public override string ToString()
        {
            return $"{Path.GetFileName(filepath)} - {FileSize.FormatSize(new FileInfo(filepath).Length)}";
        }
    }
}
