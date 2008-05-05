using System;
using LiftIO.Merging.XmlMerge;
using NUnit.Framework;

namespace LiftIO.Tests.Merging
{
    [TestFixture]
    public class XmlMergerTests
    {
        [Test]
        public void OneAddedNewMulticElement()
        {
            string red = @"<a/>";
            string ancestor = red;

            string blue = @"<a>
                                <b key='one'>
                                    <c>first</c>
                                </b>
                            </a>";

            CheckBothWaysNoConflicts(red, blue, ancestor, "a/b[@key='one']/c[text()='first']");
        }

        private void CheckBothWaysNoConflicts(string red, string blue, string ancestor, params string[] xpaths)
        {
            MergeResult r = CheckOneWay(red, blue, ancestor, xpaths);
            AssertNoConflicts(r);

            r = CheckOneWay(blue, red, ancestor, xpaths);
            AssertNoConflicts(r);
        }

        private static void AssertNoConflicts(MergeResult r)
        {
            if (r.Conflicts.Count > 0)
            {
                foreach (IConflict conflict in r.Conflicts)
                {
                    Console.WriteLine("*Unexpected: "+ conflict.GetFullHumanReadableDescription());
                }
            }
            Assert.AreEqual(0, r.Conflicts.Count, "There were unexpected conflicts.");
        }

        private MergeResult CheckOneWay(string ours, string theirs, string ancestor, params string[] xpaths)
        {
            XmlMerger m = new XmlMerger();
            m._mergeStrategies._elementStrategies.Add("a", ElementStrategy.CreateForKeyedElement("key"));
            m._mergeStrategies._elementStrategies.Add("b", ElementStrategy.CreateForKeyedElement("key"));
            m._mergeStrategies._elementStrategies.Add("c", ElementStrategy.CreateForKeyedElement("key"));
            MergeResult result = m.Merge(ours, theirs, ancestor);
            foreach (string xpath in xpaths)
            {
                XmlTestHelper.AssertXPathMatchesExactlyOne(result.MergedNode, xpath);
            }
            return result;
        }

        [Test]
        public void TextElement_OneAdded()
        {
            CheckBothWaysNoConflicts("<r><t>hello</t></r>", "<r/>", "<r/>",
                "r/t[text()='hello']");

            CheckBothWaysNoConflicts("<r><t>hello</t></r>", "<r><t/></r>", "<r/>",
                "r/t[text()='hello']");

            CheckBothWaysNoConflicts("<r><t>hello</t></r>", "<r><t/></r>", "<r><t/></r>",
                "r/t[text()='hello']");
        }

        [Test]
        public void TextElement_BothDeleted()
        {
            CheckBothWaysNoConflicts("<r><t/></r>", "<r><t></t></r>", "<r><t>hello</t></r>",
                "r/t[not(text())]",
                "r[count(t)=1]");
        }

        [Test]
        public void TextElement_OneEditted()
        {
            CheckBothWaysNoConflicts("<r><t>after</t></r>", "<r><t>before</t></r>", "<r><t>before</t></r>",
                         "r/t[contains(text(),'after')]");
        }

        [Test, Ignore("Not yet. The matcher using xmldiff sees the parent objects as different")]
        public void TextElement_BothEditted_OuterWhiteSpaceIgnored()
        {
            CheckBothWaysNoConflicts("<r><t>   flub</t></r>", "<r><t> flub      </t></r>", "<r><t/></r>",
                "r/t[contains(text(),'flub')]");
        }

        [Test]
        public void TextElement_EachEditted_OursKept_ConflictRegistered()
        {
            string ancestor = @"<t>original</t>";
            string ours = @"<t>mine</t>";
            string theirs = @"<t>theirs</t>";

            XmlMerger m = new XmlMerger();
            MergeResult result = m.Merge(ours, theirs, ancestor);
            XmlTestHelper.AssertXPathMatchesExactlyOne(result.MergedNode, "t[text()='mine']");
            Assert.AreEqual(typeof (BothEdittedTextConflict), result.Conflicts[0].GetType());
        }

        [Test]
        public void TextElement_WeEdittedTheyDeleted_OursKept_ConflictRegistered()
        {
            string ancestor = @"<t>original</t>";
            string ours = @"<t>mine</t>";
            string theirs = @"<t></t>";

            XmlMerger m = new XmlMerger();
            MergeResult result = m.Merge(ours, theirs, ancestor);
            XmlTestHelper.AssertXPathMatchesExactlyOne(result.MergedNode, "t[text()='mine']");

            Assert.AreEqual(typeof(RemovedVsEdittedTextConflict), result.Conflicts[0].GetType());
        }


        [Test]
        public void EachAddedDifferentSyblings_GetBoth()
        {

            string ancestor = @"<a/>";
            string red = @"<a>
                                <b key='one'>
                                    <c>first</c>
                                </b>
                            </a>";

            string blue = @"<a>
                                <b key='two'>
                                    <c>second</c>
                                </b>
                            </a>";

            CheckBothWaysNoConflicts(red, blue, ancestor,
                "a/b[@key='one']/c[text()='first']",
                "a/b[@key='two']/c[text()='second']");
        }

        [Test]
        public void OneAddedASyblingElement_GetBoth()
        {
            string red = @"<a>
                                <b key='one'>
                                    <c>first</c>
                                </b>
                            </a>";

            string ancestor = red;

            string blue = @"<a>
                                <b key='one'>
                                    <c>first</c>
                                </b>
                                <b key='two'>
                                    <c>second</c>
                                </b>
                            </a>";

            CheckBothWaysNoConflicts(red, blue, ancestor,
                "a/b[@key='one']/c[text()='first']",
                "a/b[@key='two']/c[text()='second']");
        }

        [Test]
        public void OneAddedSomethingDeep()
        {
            string red = @"<a>
                                <b key='one'/>
                            </a>";

            string ancestor = red;

            string blue = @"<a>
                                <b key='one'>
                                    <c>first</c>
                                </b>
                            </a>";

            Assert.IsFalse(Utilities.AreXmlElementsEqual(red, blue));

            CheckBothWaysNoConflicts(red, blue, ancestor,
                "a/b[@key='one']/c[text()='first']");
        }

        [Test]
        public void OnePutTextContentInPreviouslyElement()
        {
            string red = @"<a>
                                <b key='one'><c/></b>
                            </a>";

            string ancestor = red;


            string blue = @"<a>
                                <b key='one'>
                                    <c>first</c>
                                </b>
                            </a>";

            CheckBothWaysNoConflicts(red, blue, ancestor,
                "a/b[@key='one']/c[text()='first']");
        }

        [Test]
        public void WeDeletedAnElement_Removed()
        {
            string blue = @"<a>
                                <b key='one'>
                                    <c>first</c>
                                </b>
                            </a>";
            string ancestor = blue;

            string red = @"<a></a>";

            CheckOneWay(blue, red, ancestor, "a[ not(b)]");
        }

        [Test]
        public void TheyDeleteAnElement_Removed()
        {
            string red = @"<a></a>";
            string blue = @"<a>
                                <b key='one'>
                                    <c>first</c>
                                </b>
                            </a>";
            string ancestor = blue;

            CheckOneWay(blue, red, ancestor, "a[ not(b)]");
        }


        [Test]
        public void OneAddedAttribute()
        {
            string red = @"<a/>";
            string ancestor = red;
            string blue = @"<a one='1'/>";

            CheckBothWaysNoConflicts(blue, red, ancestor, "a[@one='1']");
        }

        [Test]
        public void BothAddedSameAttributeSameValue()
        {
            string ancestor = @"<a/>";
            string red = @"<a one='1'/>";
            string blue = @"<a one='1'/>";

            CheckBothWaysNoConflicts(blue, red, ancestor, "a[@one='1']");
        }

        [Test]
        public void BothAddedSameAttributeDifferentValue()
        {
            string ancestor = @"<a/>";
            string red = @"<a one='r'/>";
            string blue = @"<a one='b'/>";

            MergeResult r = CheckOneWay(blue, red, ancestor, "a[@one='b']");
            Assert.AreEqual(typeof(BothEdittedAttributeConflict), r.Conflicts[0].GetType());

            r =CheckOneWay(red, blue, ancestor, "a[@one='r']"); 
            Assert.AreEqual(typeof(BothEdittedAttributeConflict), r.Conflicts[0].GetType());
        }

        [Test]
        public void OneRemovedAttribute()
        {
            string red = @"<a one='1'/>";
            string ancestor = red;
            string blue = @"<a/>";

            CheckBothWaysNoConflicts(blue, red, ancestor, "a[not(@one)]");
        }
        [Test]
        public void OneMovedAndChangedAttribute()
        {
            string red = @"<a one='1' two='2'/>";
            string ancestor = red;
            string blue = @"<a two='22' one='1'/>";

            CheckBothWaysNoConflicts(blue, red, ancestor, "a[@one='1' and @two='22']");
        }

        [Test]
        public void BothAddedAnUnkeyableNephewElement()
        {
            string ancestor = @"<a>
                                <b key='one'>
                                    <cx>first</cx>
                                </b>
                            </a>";

            string red = @"<a>
                                <b key='one'>
                                    <cx>first</cx>
                                    <cx>second</cx>
                                </b>
                            </a>";


            string blue = @"<a>
                                <b key='one'>
                                    <cx>first</cx>
                                    <cx>third</cx>
                                </b>
                            </a>";

            CheckOneWay(red, blue, ancestor,
                "a[count(b)='1']",
                "a/b[count(cx)='3']",
                "a/b[@key='one']/cx[text()='first']",
                "a/b[@key='one']/cx[text()='second']",
                "a/b[@key='one']/cx[text()='third']");

            CheckOneWay(blue, red, ancestor,
         "a[count(b)='1']",
         "a/b[count(cx)='3']",
         "a/b[@key='one']/cx[text()='first']",
         "a/b[@key='one']/cx[text()='second']",
         "a/b[@key='one']/cx[text()='third']");
        
        }


        [Test]
        public void BothAddedANephewElementWithKeyAttr()
        {
            string ancestor = @"<a>
                                <b key='one'>
                                    <c key='x'>first</c>
                                </b>
                            </a>";

            string red = @"<a>
                                <b key='one'>
                                    <c key='x'>first</c>
                                    <c key='y'>second</c>
                                </b>
                            </a>";


            string blue = @"<a>
                                <b key='one'>
                                    <c key='x'>first</c>
                                    <c key='z'>third</c>
                                </b>
                            </a>";

            CheckBothWaysNoConflicts(red, blue, ancestor,
                "a[count(b)='1']",
                "a/b[count(c)='3']",
                "a/b[@key='one']/c[text()='first']",
                "a/b[@key='one']/c[text()='second']",
                "a/b[@key='one']/c[text()='third']");
        }
    }
}
