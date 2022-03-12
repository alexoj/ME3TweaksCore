using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LegendaryExplorerCore.Helpers;
using ME3TweaksCore.Localization;

namespace ME3TweaksCore.Objects
{
    public class InstalledTextureMod
    {
        public enum InstalledTextureModType
        {
            /// <summary>
            /// A file that was not tied to the ALOT Installer manifest
            /// </summary>
            USERFILE,
            /// <summary>
            /// A file that was part of the ALOT Installer manifest (OT only)
            /// </summary>
            MANIFESTFILE
        }

        public InstalledTextureModType ModType { get; set; }

        /// <summary>
        /// The listed name of the Texture Mod (manifest name if MANIFESTFILE, filename if USERFILE)
        /// </summary>
        public string ModName { get; set; }
        /// <summary>
        /// The author of the texture mod. Only written to the marker if the mod is a MANIFESTFILE.
        /// </summary>
        public string AuthorName { get; set; }

        /// <summary>
        /// The UI string to display for this mod.
        /// </summary>
        public string UIName
        {
            get
            {
                var ret = ModName;
                if (ModType == InstalledTextureModType.MANIFESTFILE)
                {
                    ret = LC.GetString(LC.string_interp_modNameByAuthorName, ModName, AuthorName);
                }
                return ret;
            }
        }

        /// <summary>
        /// Options that were chosen for this mod at install time
        /// </summary>
        public List<string> ChosenOptions { get; } = new List<string>();

        ///// <summary>
        ///// Generates an InstalledTextureMod object from the given installer file. PreinstallMod objects are not supported.
        ///// </summary>
        ///// <param name="ifx"></param>
        //public InstalledTextureMod(InstallerFile ifx)
        //{
        //    if (ifx is PreinstallMod) throw new Exception(@"PreinstallMod is not a type of texture mod!");
        //    ModType = ifx is UserFile ? ModType = InstalledTextureModType.USERFILE : InstalledTextureModType.MANIFESTFILE;
        //    ModName = ifx.FriendlyName;
        //    if (ifx is ManifestFile mf)
        //    {
        //        AuthorName = ifx.Author;
        //        if (mf.ChoiceFiles.Any())
        //        {
        //            Debug.WriteLine("hi");
        //        }

        //        foreach (var cf in mf.ChoiceFiles)
        //        {
        //            var chosenFile = cf.GetChosenFile();
        //            if (chosenFile != null)
        //            {
        //                ChosenOptions.Add($"{cf.ChoiceTitle}: {chosenFile.ChoiceTitle}");
        //            }
        //        }
        //    }
        //}


        /// <summary>
        /// Reads installed texture mod info from the stream, based on the marker version
        /// </summary>
        /// <param name="inStream"></param>
        /// <param name="extendedMarkerVersion"></param>
        public InstalledTextureMod(Stream inStream, int extendedMarkerVersion)
        {
            // V4 marker version - DEFAULT (Extended Version 2)
            ModType = (InstalledTextureModType)inStream.ReadByte();
            ModName = extendedMarkerVersion == 0x02 ? inStream.ReadStringUnicodeNull() : inStream.ReadUnrealString();
            if (ModType == InstalledTextureModType.MANIFESTFILE)
            {
                AuthorName = extendedMarkerVersion == 0x02 ? inStream.ReadStringUnicodeNull() : inStream.ReadUnrealString();
                var numChoices = inStream.ReadInt32();
                while (numChoices > 0)
                {
                    ChosenOptions.Add(inStream.ReadStringUnicodeNull());
                    numChoices--;
                }
            }
        }

        public InstalledTextureMod()
        {

        }

        /// <summary>
        /// Writes this object's information to the stream, using the latest texture mod marker format.
        /// </summary>
        /// <param name="fs"></param>
        public void WriteToMarker(Stream fs)
        {
            fs.WriteByte((byte)ModType); // user file = 0, manifest file = 1
            fs.WriteStringUnicodeNull(ModName);
            if (ModType == InstalledTextureModType.MANIFESTFILE)
            {
                fs.WriteStringUnicodeNull(AuthorName);
                fs.WriteInt32(ChosenOptions.Count);
                foreach (var c in ChosenOptions)
                {
                    fs.WriteStringUnicodeNull(c);
                }
            }
        }
    }
}
