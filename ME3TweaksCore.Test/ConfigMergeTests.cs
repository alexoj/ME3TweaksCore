using LegendaryExplorerCore.Coalesced;
using LegendaryExplorerCore.Helpers;
using LegendaryExplorerCore.Misc;
using LegendaryExplorerCore.Packages;
using ME3TweaksCore.Config;
using ME3TweaksCore.Diagnostics;
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

        [TestMethod]
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
    }
}