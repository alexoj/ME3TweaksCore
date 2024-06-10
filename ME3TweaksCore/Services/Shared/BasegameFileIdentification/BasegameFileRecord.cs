using System;
using System.Collections.Generic;
using System.IO;
using LegendaryExplorerCore.Gammtek.Extensions;
using LegendaryExplorerCore.Helpers;
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


        // Data block - used so we can add and remove blocks from the record text. It's essentially a crappy struct.
        public static readonly string BLOCK_OPENING = @"[[";
        public static readonly string BLOCK_CLOSING = @"]]";

        public static string CreateBlock(string blockName, string blockData)
        {
            return $"{BLOCK_OPENING}{blockName}={blockData}{BLOCK_CLOSING}";
        }

        public string GetWithoutBlock(string blockName)
        {
            string parsingStr = source;
            int openIdx = parsingStr.IndexOf(BLOCK_OPENING);
            int closeIdx = parsingStr.IndexOf(BLOCK_CLOSING);
            while (openIdx >= 0 && closeIdx >= 0 && openIdx > closeIdx)
            {
                var blockText = parsingStr.Substring(openIdx + BLOCK_OPENING.Length, closeIdx - (openIdx + BLOCK_OPENING.Length));
                var blockEqIdx = blockText.IndexOf('=');
                if (blockEqIdx > 0)
                {
                    var pBlockName = blockText.Substring(0, blockEqIdx);
                    if (pBlockName.CaseInsensitiveEquals(blockName))
                    {
                        // The lazy way: Just do a replacement with nothing.
                        return source.Replace(parsingStr.Substring(openIdx, closeIdx - (openIdx + BLOCK_OPENING.Length + BLOCK_CLOSING.Length)), @"");
                    }
                }
            }

            // There is edge case where you have ]][[ in the string. Is anyone going to do that? Please don't.
            // We did not find the data block.
            return parsingStr;
        }
    }
}
