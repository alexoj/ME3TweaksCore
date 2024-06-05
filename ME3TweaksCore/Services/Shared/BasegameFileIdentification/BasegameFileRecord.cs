using System;
using System.Collections.Generic;
using System.IO;
using LegendaryExplorerCore.Gammtek.Extensions;
using LegendaryExplorerCore.Packages;
using ME3TweaksCore.Helpers;
using ME3TweaksCore.Targets;

namespace ME3TweaksCore.Services.Shared.BasegameFileIdentification
{
    /// <summary>
    /// Information that can be used to identify the source of the mod for a basegame file change.
    /// </summary>
    public class BasegameFileRecord
    {
        public string file { get; set; }
        public string hash { get; set; }
        public string source { get; set; }
        public string game { get; set; }
        public int size { get; set; }
        public List<string> moddeschashes { get; set; } = new List<string>(4);
        public BasegameFileRecord() { }
        public BasegameFileRecord(string relativePathToRoot, int size, MEGame game, string humanName, string md5)
        {
            file = relativePathToRoot;
            hash = md5 ?? MUtilities.CalculateHash(relativePathToRoot);
            this.game = game.ToGameNum().ToString(); // due to how json serializes stuff we have to convert it here.
            this.size = size;
            source = humanName;
        }

        /// <summary>
        /// Generates a basegame file record from the file path, the given target, and the display name
        /// </summary>
        /// <param name="fullfilepath">The full file path, not a relative one</param>
        /// <param name="target"></param>
        /// <param name="recordedMergeName"></param>
        /// <exception cref="NotImplementedException"></exception>
        public BasegameFileRecord(string fullfilepath, GameTarget target, string recordedMergeName)
        {
            this.file = fullfilepath.Substring(target.TargetPath.Length + 1);
            this.hash = MUtilities.CalculateHash(fullfilepath);
            this.game = target.Game.ToGameNum().ToString();
            this.size = (int)new FileInfo(fullfilepath).Length;
            this.source = recordedMergeName;
        }
    }
}
