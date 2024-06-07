using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LegendaryExplorerCore.Misc;
using ME3TweaksCore.Helpers;
using ME3TweaksCore.Misc;

namespace ME3TweaksCore.Test
{
    internal class MultiParserResult
    {
        public string InputString { get; set; }
        public CaseInsensitiveDictionary<List<string>> ExpectedResults { get; set; }
        public char OpenChar { get; set; }
        public char CloseChar { get; set; }
        public bool IsBad { get; set; }
        private MultiParserResult() { }

        public static MultiParserResult MakeBad(string input, char openChar, char closeChar,
            List<(string key, string val)> results)
        {
            var res = Make(input, openChar, closeChar, results);
            res.IsBad = true;
            return res;
        }

        public static MultiParserResult Make(string input, char openChar, char closeChar, List<(string key, string val)> results)
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
                MultiParserResult.Make(@"(Attribute1=Value1)", '(', ')', [("Attribute1", "Value1")]),
                MultiParserResult.Make(@"[Attribute1=Value1]", '[', ']', [("Attribute1", "Value1")]),
                MultiParserResult.Make(@"(Attribute1 = Value1)", '(', ')', [("Attribute1", "Value1")]),
                MultiParserResult.Make(@"(Attribute1=Value1);", '(', ')', [("Attribute1", "Value1")]),
            
                // Medium
                MultiParserResult.Make(@"(MedAttribute1=Kite, MedAttribute2=Bird)", '(', ')',
                    [("MedAttribute1", "Kite"), ("MedAttribute2", "Bird")]),
                MultiParserResult.Make(@"(MedAttribute1=Kite, MedAttribute1=Bird)", '(', ')',
                    [("MedAttribute1", "Kite"), ("MedAttribute1", "Bird")]),

                // Hard
                MultiParserResult.Make(@"(HardAttribute1=(Kite, Dog),HardAttribute1 = (Fly like a plane))", '(', ')',
                    [("HardAttribute1", "(Kite, Dog)"), ("HardAttribute1", "(Fly like a plane)")]),

                // Practical
                MultiParserResult.Make(@"(MinVersion=0.99, Option=(Key=DB8CAA13, UIString=Expanded Galaxy Mod Normandy Module))", '(', ')',
                    [("MinVersion", "0.99"), ("Option", "(Key=DB8CAA13, UIString=Expanded Galaxy Mod Normandy Module)")]),
                MultiParserResult.Make(@"[minversion=0.99, option=[Key=+DB8CAA13, UIString=Expanded Galaxy Mod Normandy Module[Overhauled Edition]], option=[Key=-BetaFeature, UIString=Galactic War Module]]", '[', ']',
                    [("minversion", "0.99"),
                        ("option", "[Key=+DB8CAA13, UIString=Expanded Galaxy Mod Normandy Module[Overhauled Edition]]"),
                        ("option", "[Key=-BetaFeature, UIString=Galactic War Module]")
                    ]),

                // Probably not what user wants but should still parse.
                
                MultiParserResult.Make(@"[minversion=0.99, option=[Key=+MirMod, UIString=Miranda Mod [Overhauled Edition]], option=[Key=-BigHappyCloud, UIString=DoYouLikeBracketsAtTheEnd[]]]", '[', ']',
                [("minversion", "0.99"),
                    ("option", "[Key=+MirMod, UIString=Miranda Mod [Overhauled Edition]]"),
                    ("option", "[Key=-BigHappyCloud, UIString=DoYouLikeBracketsAtTheEnd[]]")
                ]),

                // Bad. Should not parse

                // Has non-prop on end
                MultiParserResult.MakeBad(@"[minversion=0.99, option=[Key=+DB8CAA13, UIString=Expanded Galaxy Mod Normandy Module[Overhauled Edition]], option=[Key=-BetaFeature, UIString=Galactic War Module[]]], extra trash on the end", '[', ']',
                [("minversion", "0.99"),
                    ("option", "[Key=+DB8CAA13, UIString=Expanded Galaxy Mod Normandy Module[[[[[Overhauled Edition]"),
                    ("option", "[Key=-BetaFeature, UIString=Galactic War Module[]")
                ]),
                // Has too many ] on the end.
                MultiParserResult.MakeBad(@"[minversion=0.99, option=[Key=+DB8CAA13, UIString=Expanded Galaxy Mod Normandy Module[Overhauled Edition]], option=[Key=-BetaFeature, UIString=Galactic War Module]]]", '[', ']',
                [("minversion", "0.99"),
                    ("option", "[Key=+DB8CAA13, UIString=Expanded Galaxy Mod Normandy Module[Overhauled Edition]]"),
                    ("option", "[Key=-BetaFeature, UIString=Galactic War Module]")
                ]),

        ];

        [TestMethod]
        public void TestParsing()
        {
            // MULTI VALUE MAP TESTS
            foreach (var test in Tests)
            {

                Dictionary<string, List<string>> split = null;

                try
                {
                    split = StringStructParser.GetSplitMultiMapValues(test.InputString, true, test.OpenChar,
                        test.CloseChar);
                }
                catch (Exception ex)
                {
                    if (!test.IsBad)
                    {
                        Assert.Fail($"An exception was thrown that should not have been! {ex.Message}");
                    }

                    // Should have failed. We can safely skip the remainder of the test
                    continue;
                }

                // Test we got the same number of keys
                Assert.IsTrue(split.Count == test.ExpectedResults.Count);

                // Test we got the correct values
                foreach (var parsedKey in split)
                {
                    if (test.ExpectedResults.TryGetValue(parsedKey.Key, out var expectedValues))
                    {
                        Assert.IsTrue(expectedValues.Count == parsedKey.Value.Count);
                        foreach (var val in expectedValues)
                        {
                            if (test.IsBad)
                            {

                            }
                            Assert.IsTrue(parsedKey.Value.Contains(val));
                        }
                    }
                    else
                    {
                        Assert.Fail($"Expected results did not contain key {parsedKey.Key}");
                    }
                }
            }
        }
    }
}
