using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using LiftIO.Merging;
using LiftIO.Merging.XmlMerge;
using NUnit.Framework;
using XmlUnit;

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

            CheckBothWays(red, blue, ancestor, "a/b[@key='one']/c[text()='first']");
        }

        private void CheckBothWays(string red, string blue, string ancestor, params string[] xpaths)
        {
            CheckOneWay(red, blue, ancestor, xpaths);
            CheckOneWay(blue, red, ancestor, xpaths);
        }

        private void CheckOneWay(string ours, string theirs, string ancestor, params string[] xpaths)
        {
            XmlMerger m = new XmlMerger(new ConsolMergeLogger());
            m._mergeStrategies._elementStrategies.Add("a", ElementStrategy.CreateForKeyedElement("key"));
            m._mergeStrategies._elementStrategies.Add("b", ElementStrategy.CreateForKeyedElement("key"));
            m._mergeStrategies._elementStrategies.Add("c", ElementStrategy.CreateForKeyedElement("key"));
            string result = m.Merge(ours, theirs, ancestor);
            foreach (string xpath in xpaths)
            {
                XmlTestHelper.AssertXPathMatchesExactlyOne(result, xpath);
            }
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

            CheckBothWays(red, blue, ancestor,
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

            CheckBothWays(red, blue, ancestor,
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

            CheckBothWays(red, blue, ancestor,
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

            CheckBothWays(red, blue, ancestor,
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

            CheckBothWays(blue, red, ancestor, "a[@one='1']");
        }

        [Test]
        public void BothAddedSameAttributeSameValue()
        {
            string ancestor = @"<a/>";
            string red = @"<a one='1'/>";
            string blue = @"<a one='1'/>";

            CheckBothWays(blue, red, ancestor, "a[@one='1']");
        }
        [Test]
        public void BothAddedSameAttributeDifferentValue()
        {
            string ancestor = @"<a/>";
            string red = @"<a one='r'/>";
            string blue = @"<a one='b'/>";

            CheckOneWay(blue, red, ancestor, "a[@one='b']");  //todo conflict
            CheckOneWay(red, blue, ancestor, "a[@one='r']"); //todo conflict
        }
        [Test]
        public void OneRemovedAttribute()
        {
            string red = @"<a one='1'/>";
            string ancestor = red;
            string blue = @"<a/>";

            CheckBothWays(blue, red, ancestor, "a[not(@one)]");
        }
        [Test]
        public void OneMovedAndChangedAttribute()
        {
            string red = @"<a one='1' two='2'/>";
            string ancestor = red;
            string blue = @"<a two='22' one='1'/>";

            CheckBothWays(blue, red, ancestor, "a[@one='1' and @two='22']");
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

            CheckBothWays(red, blue, ancestor,
                "a[count(b)='1']",
                "a/b[count(c)='3']",
                "a/b[@key='one']/c[text()='first']",
                "a/b[@key='one']/c[text()='second']",
                "a/b[@key='one']/c[text()='third']");
        }
    }
}
