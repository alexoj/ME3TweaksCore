using System.Diagnostics;
using System.Security.Cryptography;
using System.Security.Policy;
using LegendaryExplorerCore;
using LegendaryExplorerCore.Coalesced;
using LegendaryExplorerCore.Compression;
using LegendaryExplorerCore.GameFilesystem;
using LegendaryExplorerCore.Gammtek.IO;
using LegendaryExplorerCore.Helpers;
using LegendaryExplorerCore.Misc;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Unreal;
using ME3TweaksCore.Diagnostics;
using ME3TweaksCore.Helpers;
using Serilog;

namespace ME3TweaksCore.Test
{
    [TestClass]
    public class ConfigMergeTests
    {
        public static void GlobalInit()
        {
#if !AZURE
            Log.Logger = new LoggerConfiguration().WriteTo.Console().WriteTo.Debug().CreateLogger();
#else
            Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateLogger();
#endif
        }

        // [TestMethod]
        public void TestConfigMerge()
        {
            // WIP: This is for dev right now
            //GlobalInit();
            //var coalBaseFile = @"G:\My Drive\Mass Effect Legendary Modding\CoalescedMerge\Coalesced_INT.bin";
            //var coalOut = @"G:\My Drive\Mass Effect Legendary Modding\CoalescedMerge\Coalesced_INT_OUT.bin";
            //var deltaBaseFile = @"G:\My Drive\Mass Effect Legendary Modding\CoalescedMerge\CoalDelta.m3cd"; // M3 Coalesced Delta file

            //// Read coalesced
            //using var coalFS = File.OpenRead(coalBaseFile);
            //var coal = ConfigAssetBundle.FromSingleFile(MEGame.LE1,coalBaseFile)

            //// Read delta
            //var delta = ConfigFileProxy.LoadIni(deltaBaseFile);

            //ConfigMerge.PerformMerge(coal, delta);
            //CaseInsensitiveDictionary<DuplicatingIni> inis = new CaseInsensitiveDictionary<DuplicatingIni>();

            //var compiled = CoalescedConverter.CompileLE1LE2FromMemory(inis);
            //compiled.WriteToFile(coalOut);
        }


        // Data types for the local profile settings.
        private const int SETTINGTYPE_EMPTY = 0;
        private const int SETTINGTYPE_INT = 1;
        private const int SETTINGTYPE_INT64 = 2;
        private const int SETTINGTYPE_DOUBLE = 3;
        private const int SETTINGTYPE_STRING = 4;
        private const int SETTINGTYPE_FLOAT = 5;
        private const int SETTINGTYPE_BLOB = 6;
        private const int SETTINGTYPE_DATETIME = 7;
        private const int SETTINGTYPE_MAX = 8;


        public class ProfileSEtting
        {
            public int id { get; set; }
            public object value1 { get; set; }
            public string Name { get; set; }

        }

        [TestMethod]
        public void SaveTest()
        {
            LegendaryExplorerCoreLib.InitLib(null);
            var localProfilePath = Path.Combine(LE3Directory.BioWareDocumentsPath, @"Save", @"ME3", @"Local_Profile");

            if (!File.Exists(localProfilePath))
            {
                Debug.WriteLine("LOCAL_PROFILE NOT FOUND");
                return;
            }
            var bytes = File.ReadAllBytes(localProfilePath);
            var streamc = new EndianReader(new MemoryStream(bytes)) { Endian = Endian.Big };
            var sha = streamc.ReadToBuffer(20);
            var decompressedSize = streamc.ReadInt32();
            var remaining = streamc.ReadToBuffer((int)streamc.Length - (int)streamc.Position);

            var byted = new byte[decompressedSize];
            var decompressedProfile = OodleHelper.Decompress(remaining, byted);


            // read settings mapping
            var sfxgame = MEPackageHandler.OpenMEPackage(LE3Directory.CookedPCPath + @"\SFXGame.pcc");
            var props = sfxgame.FindExport(@"Default__SFXProfileSettings").GetProperties();

            Dictionary<int, string> idToName = new Dictionary<int, string>();
            var profMappings = props.GetProp<ArrayProperty<StructProperty>>("ProfileMappings");
            foreach (var prof in profMappings)
            {
                idToName[prof.GetProp<IntProperty>(@"id")] = prof.GetProp<NameProperty>("Name").Value.Instanced;
            }
            // Parser for Local_Profile

            var rest = MUtilities.CalculateHash(new MemoryStream(remaining), @"sha1"); // Compressed original, SHA1 after first 32 bytes
            var sha1 = BitConverter.ToString(sha).Replace(@"-", "").ToLowerInvariant(); // The listed SHA value at top of Local_Profile
            var sha2 = MUtilities.CalculateHash(new MemoryStream(decompressedProfile), @"sha1"); // Decompressed SHA1



            var stream = new EndianReader(new MemoryStream(decompressedProfile)) { Endian = Endian.Big };

            var numSettings = stream.ReadInt32();
            int counted = 0;
            List<ProfileSEtting> settins = new();
            for (int i = 0; i < numSettings; i++)
            {
                var setting = new ProfileSEtting();
                stream.ReadByte(); // Should be 0x1 INT
                setting.id = stream.ReadInt32();
                if (idToName.ContainsKey(setting.id))
                {
                    setting.Name = idToName[setting.id];
                }
                else
                {
                    setting.Name = $"UNMAPPED KEY: {setting.id}";
                }

                settins.Add(setting);
                counted++;

                // Each setting is 3 values
                object outVal = null;
                // Draw the rest of the owl
                Debug.Write($"0x{stream.Position:X8}: ");
                var type = stream.ReadByte();
                switch (type)
                {
                    case SETTINGTYPE_EMPTY:
                        break;
                    case SETTINGTYPE_INT:
                        var val = stream.ReadInt32();
                        //Debug.WriteLine($"{i}\tINT: {val}");
                        outVal = val;
                        break;
                    case SETTINGTYPE_INT64:
                        var val64 = stream.ReadInt32(); //stream.ReadInt64();
                        //Debug.WriteLine($"{i}\tINT64: {val64}");
                        outVal = val64;
                        break;
                    case SETTINGTYPE_DOUBLE:
                        var vald = stream.ReadDouble();
                        outVal = vald;

                        //Debug.WriteLine($"{i}\tDOUBLE: {vald}");
                        break;
                    case SETTINGTYPE_STRING:
                        var vals = stream.ReadUnrealString();
                        outVal = vals;
                        //Debug.WriteLine($"{i}\tSTRING: {vals}");
                        break;
                    case SETTINGTYPE_FLOAT:
                        var valf = stream.ReadFloat();
                        outVal = valf;
                        //Debug.WriteLine($"{i}\tFLOAT: {valf}");
                        break;
                    case SETTINGTYPE_BLOB:
                        var blobSize = stream.ReadInt32();
                        var blob = stream.ReadToBuffer(blobSize);
                        outVal = blob;
                        //Debug.WriteLine($"{i}\tBLOB (size: {blobSize})");
                        break;
                    case SETTINGTYPE_DATETIME:
                        // ???????
                        var val1 = stream.ReadInt32();
                        var val2 = stream.ReadInt32();
                        outVal = (val1, val2);
                        //Debug.WriteLine($"{i}\tDATETIME {val1} {val2}");
                        break;
                    case SETTINGTYPE_MAX:
                        // ??
                        break;
                    default:
                        Debug.WriteLine($"ERROR: TYPE {type}");
                        break;
                }

                setting.value1 = outVal;
                stream.ReadByte(); // Should be 0x0
                Debug.WriteLine($@"Setting {setting.id} {setting.Name}: VALUE: {setting.value1}");
            }

            Debug.WriteLine($@"Final position: 0x{stream.Position:X8}, stream len: 0x{stream.Length:X8}");
        }
    }
}