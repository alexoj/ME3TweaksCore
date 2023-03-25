using LegendaryExplorerCore.Coalesced;
using LegendaryExplorerCore.Helpers;
using LegendaryExplorerCore.Misc;
using ME3TweaksCore.Config;

namespace ME3TweaksCore.Test
{
    [TestClass]
    public class ConfigMergeTests
    {
        [TestMethod]
        public void TestConfigMerge()
        {
            // WIP: This is for dev right now

            var coalBaseFile = @"B:\UserProfile\Documents\Coalesced_INT.bin"; // LE1
            var coalOut = @"B:\UserProfile\Documents\Coalesced_INT_OUT.bin"; // LE1
            var deltaBaseFile = @"B:\UserProfile\Documents\CoalDelta.m3cd"; // M3 Coalesced Delta file

            // Read coalesced
            using var coalFS = File.OpenRead(coalBaseFile);
            var coal = CoalescedConverter.DecompileLE1LE2ToAssets(coalFS, Path.GetFileName(coalBaseFile));

            // Read delta
            var delta = ConfigFileProxy.LoadIni(deltaBaseFile);

            ConfigMerge.PerformMerge(coal, delta);
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