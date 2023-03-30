using System.Diagnostics;
using LegendaryExplorerCore.Coalesced;
using LegendaryExplorerCore.Compression;
using LegendaryExplorerCore.Gammtek.IO;
using LegendaryExplorerCore.Helpers;
using LegendaryExplorerCore.Misc;
using LegendaryExplorerCore.Packages;
using ME3TweaksCore.Config;
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
            MLog.SetLogger(Log.Logger);
        }

        // [TestMethod]
        public void TestConfigMerge()
        {
            // WIP: This is for dev right now
            GlobalInit();
            var coalBaseFile = @"G:\My Drive\Mass Effect Legendary Modding\CoalescedMerge\Coalesced_INT.bin";
            var coalOut = @"G:\My Drive\Mass Effect Legendary Modding\CoalescedMerge\Coalesced_INT_OUT.bin";
            var deltaBaseFile = @"G:\My Drive\Mass Effect Legendary Modding\CoalescedMerge\CoalDelta.m3cd"; // M3 Coalesced Delta file

            // Read coalesced
            using var coalFS = File.OpenRead(coalBaseFile);
            var coal = CoalescedConverter.DecompileLE1LE2ToAssets(coalFS, Path.GetFileName(coalBaseFile));

            // Read delta
            var delta = ConfigFileProxy.LoadIni(deltaBaseFile);

            ConfigMerge.PerformMerge(coal, delta, MEGame.LE1);
            CaseInsensitiveDictionary<DuplicatingIni> inis = new CaseInsensitiveDictionary<DuplicatingIni>();
            foreach (var asset in coal)
            {
                inis[asset.Key] = CoalesceAsset.ToIni(asset.Value);
            }

            var compiled = CoalescedConverter.CompileLE1LE2FromMemory(inis);
            compiled.WriteToFile(coalOut);
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

        [TestMethod]
        public void SaveTest()
        {
            //OodleHelper.EnsureOodleDll();
            //var f = @"B:\UserProfile\Documents\BioWare\Mass Effect Legendary Edition\Save\ME3\Local_Profile";
            //var bytes = File.ReadAllBytes(f);
            //var stream = new EndianReader(new MemoryStream(bytes)) { Endian = Endian.Big };
            //var sha = stream.ReadToBuffer(20);
            //var decompressedSize = stream.ReadInt32();
            //var remaining = stream.ReadToBuffer((int)stream.Length - (int)stream.Position);

            //var byted = new byte[decompressedSize];
            //var data = OodleHelper.Decompress(remaining, byted);
            //File.WriteAllBytes(@"B:\UserProfile\Documents\BioWare\Mass Effect Legendary Edition\Save\ME3\Local_Profile_decompressed.bin", byted);
            //Debug.WriteLine("Test");


            // Parser for Local_Profile

            var lpd = @"B:\UserProfile\Documents\BioWare\Mass Effect Legendary Edition\Save\ME3\Local_Profile_decompressed.bin";
            var data = File.ReadAllBytes(lpd);
            var stream = new EndianReader(new MemoryStream(data)) { Endian = Endian.Big };

            var numSettings = stream.ReadInt32();

            for (int i = 0; i < numSettings; i++)
            {
                // Draw the rest of the owl
                Debug.Write($"0x{stream.Position:X8}: ");
                var type = stream.ReadByte();
                switch (type)
                {
                    case SETTINGTYPE_EMPTY:
                        i--;
                        Debug.WriteLine($"{i}\tEMPTY");
                        break;
                    case SETTINGTYPE_INT:
                        var val = stream.ReadInt32();
                        Debug.WriteLine($"{i}\tINT: {val}");
                        break;
                    case SETTINGTYPE_INT64:
                        var val64 = stream.ReadInt32(); //stream.ReadInt64();
                        Debug.WriteLine($"{i}\tINT64: {val64}");
                        break;
                    case SETTINGTYPE_DOUBLE:
                        var vald = stream.ReadDouble();
                        Debug.WriteLine($"{i}\tDOUBLE: {vald}");
                        break;
                    case SETTINGTYPE_STRING:
                        var vals = stream.ReadUnrealString();
                        Debug.WriteLine($"{i}\tSTRING: {vals}");
                        break;
                    case SETTINGTYPE_FLOAT:
                        var valf = stream.ReadFloat();
                        Debug.WriteLine($"{i}\tFLOAT: {valf}");
                        break;
                    case SETTINGTYPE_BLOB:
                        var blobSize = stream.ReadInt32();
                        var blob = stream.ReadToBuffer(blobSize);
                        Debug.WriteLine($"{i}\tBLOB (size: {blobSize})");
                        break;
                    case SETTINGTYPE_DATETIME:
                        // ???????
                        var val1 = stream.ReadInt32();
                        var val2 = stream.ReadInt32();
                        Debug.WriteLine($"{i}\tDATETIME {val1} {val2}");
                        break;
                    case SETTINGTYPE_MAX:
                        // ??
                        break;
                    default:
                        Debug.WriteLine($"ERROR: TYPE {type}");
                        break;
                }

            }

            Debug.WriteLine($@"Final position: 0x{stream.Position:X8}, stream len: 0x{stream.Length:X8}");
        }
    }
}