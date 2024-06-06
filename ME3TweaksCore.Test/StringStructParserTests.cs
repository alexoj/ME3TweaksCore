using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LegendaryExplorerCore.Misc;
using ME3TweaksCore.Helpers;

namespace ME3TweaksCore.Test
{
    internal class MultiParserResult
    {
        public string InputString { get; set; }
        public CaseInsensitiveDictionary<List<string>> ExpectedResults { get; set; }
        public char OpenChar { get; set; }
        public char CloseChar { get; set; }
        private MultiParserResult() { }

        public static MultiParserResult Make(string input, char openChar, char closeChar,
            List<(string key, string val)> results)
        {
            var mappedResults = new CaseInsensitiveDictionary<List<string>>();
            foreach (var item in results)
            {
                if (!mappedResults.TryGetValue(item.key, out var list))
                {
                    list = new List<string>();
                    mappedResults[item.key] = list;
                }
                list.Add(item.val);
            }

            return new MultiParserResult()
            {
                InputString = input,
                OpenChar = openChar,
                CloseChar = closeChar,
                ExpectedResults = mappedResults,

            };
        }
    }

    [TestClass]
    public class StringStructParserTests
    {
        internal static MultiParserResult[] Tests = [
                // Easy
                MultiParserResult.Make(@"(Attribute1=Value1)", '(', ')', [("Attribute1", "Value")]),
                MultiParserResult.Make(@"[Attribute1=Value1]", '[', ']', [("Attribute1", "Value1")]),
                MultiParserResult.Make(@"(Attribute1 = Value1)", '(', ')', [("Attribute1", "Value1")]),
                MultiParserResult.Make(@"(Attribute1=Value1);", '(', ')', [("Attribute1", "Value1")]),
            
                // Medium
                MultiParserResult.Make(@"(MedAttribute1=Kite, MedAttribute2=Bird)", '(', ')', 
                    [("MedAttribute1", "Kite"), ("MedAttribute2", "Bird")]),
                MultiParserResult.Make(@"(MedAttribute1=Kite, MedAttribute1=Bird)", '(', ')',
                    [("MedAttribute1", "Kite"), ("MedAttribute1", "Bird")]),

                // Hard
                MultiParserResult.Make(@"(HardAttribute1=(Kite, Dog), HardAttribute1 = (Fly like a plane))", '(', ')',
                    [("HardAttribute1", "(Kite, Dog)"), ("HardAttribute1", "(Fly like a plane)")]),

                // Practical
                MultiParserResult.Make(@"(MinVersion=0.99, Option=(Key=DB8CAA13, UIString=Expanded Galaxy Mod Normandy Module))", '(', ')',
                    [("MinVersion", "0.99"), ("Option", "(Key=DB8CAA13, UIString=Expanded Galaxy Mod Normandy Module)")]),

        ];

        [TestMethod]
        public void TestParsing()
        {
            // MULTI VALUE MAP TESTS
            foreach (var test in Tests)
            {
                var split = StringStructParser.GetSplitMultiValues(test.InputString, true, test.OpenChar, test.CloseChar);
                Assert.IsTrue(split.Count == test.ExpectedResults.Count);
                Assert.IsTrue(split.Count == test.ExpectedResults.Count);
                foreach (var key in split)
                {
                    if (test.ExpectedResults.TryGetValue(key.Key, out var list))
                    {
                        Assert.IsTrue(list.Count == key.Value.Count);
                        foreach (var val in list)
                        {
                            Assert.IsTrue(list.Contains(val));
                        }
                    }
                    else
                    {
                        Assert.Fail($"Expected results did not contain key {key.Key}");
                    }
                }
            }
        }
    }
}
