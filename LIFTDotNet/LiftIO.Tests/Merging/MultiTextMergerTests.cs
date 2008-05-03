using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using LiftIO.Merging;
using NUnit.Framework;
using XmlUnit;

namespace LiftIO.Tests.Merging
{
    [TestFixture]
    public class MultiTextMergerTests
    {
        [Test]
        public void MergeMultiTextNodes_OneAddedNewMultiTextElement()
        {
            string red = @"";
            string ancestor = red;

            string blue = @"<lexical-unit>
                                <form lang='one'>
                                    <text>first</text>
                                </form>
                            </lexical-unit>";

            CheckBothWays(red, blue, ancestor, "lexical-unit/form[@lang='one']/text[text()='first']");
        }

        //        private void CheckBothWays(string red, string blue, string ancestor, string xpath)
        //        {
        //            XmlNode result= LiftSavvyMergeStrategy.MergeMultiTextPieces(red, blue, ancestor);
        //            XmlTestHelper.AssertXPathMatchesExactlyOne(result.OuterXml, xpath);
        //            result= LiftSavvyMergeStrategy.MergeMultiTextPieces(blue, red, ancestor);
        //            XmlTestHelper.AssertXPathMatchesExactlyOne(result.OuterXml, xpath);
        //        }

        private void CheckBothWays(string red, string blue, string ancestor, params string[] xpaths)
        {
            CheckOneWay(red, blue, ancestor, xpaths);
            CheckOneWay(blue, red, ancestor, xpaths);
        }

        private void CheckOneWay(string ours, string theirs, string ancestor, params string[] xpaths)
        {
            XmlNode result = MultiTextMerger.MergeMultiTextPieces(ours, theirs, ancestor);
            foreach (string xpath in xpaths)
            {
                XmlTestHelper.AssertXPathMatchesExactlyOne(result.OuterXml, xpath);
            }
        }

        [Test]
        public void MergeMultiTextNodes_EachAddedDifferentAlternatives_GetBoth()
        {

            string ancestor = @"<lexical-unit>
                            </lexical-unit>";


            string red = @"<lexical-unit>
                                <form lang='one'>
                                    <text>first</text>
                                </form>
                            </lexical-unit>";

            string blue = @"<lexical-unit>
                                <form lang='two'>
                                    <text>second</text>
                                </form>
                            </lexical-unit>";

            CheckBothWays(red, blue, ancestor,
                "lexical-unit/form[@lang='one']/text[text()='first']",
                "lexical-unit/form[@lang='two']/text[text()='second']");
        }

        [Test]
        public void MergeMultiTextNodes_OneAddedAnAlternatives_GetBoth()
        {
            string red = @"<lexical-unit>
                                <form lang='one'>
                                    <text>first</text>
                                </form>
                            </lexical-unit>";

            string ancestor = red;

            string blue = @"<lexical-unit>
                                <form lang='one'>
                                    <text>first</text>
                                </form>
                                <form lang='two'>
                                    <text>second</text>
                                </form>
                            </lexical-unit>";

            CheckBothWays(red, blue, ancestor,
                "lexical-unit/form[@lang='one']/text[text()='first']",
                "lexical-unit/form[@lang='two']/text[text()='second']");
        }

        [Test]
        public void MergeMultiTextNodes_OnePutSomethingInPreviouslyEmptyForm()
        {
            string red = @"<lexical-unit>
                                <form lang='one'/>
                            </lexical-unit>";

            string ancestor = red;

            string blue = @"<lexical-unit>
                                <form lang='one'>
                                    <text>first</text>
                                </form>
                            </lexical-unit>";

            Assert.IsFalse(Utilities.AreXmlElementsEqual(red, blue));

            CheckBothWays(red, blue, ancestor,
                "lexical-unit/form[@lang='one']/text[text()='first']");
        }

        [Test]
        public void MergeMultiTextNodes_OnePutSomethingInPreviouslyEmptyFormText()
        {
            string red = @"<lexical-unit>
                                <form lang='one'><text/></form>
                            </lexical-unit>";

            string ancestor = red;


            string blue = @"<lexical-unit>
                                <form lang='one'>
                                    <text>first</text>
                                </form>
                            </lexical-unit>";

            CheckBothWays(red, blue, ancestor,
                "lexical-unit/form[@lang='one']/text[text()='first']");
        }

        [Test]
        public void WeDeletedAForm_FormRemoved()
        {
            string red = @"<lexical-unit></lexical-unit>";
            string blue = @"<lexical-unit>
                                <form lang='one'>
                                    <text>first</text>
                                </form>
                            </lexical-unit>";
            string ancestor = blue;

            CheckOneWay(blue, red, ancestor, "lexical-unit[ not(form)]");
        }

        [Test]
        public void TheyDeleteAForm_FormRemoved()
        {
            string red = @"<lexical-unit></lexical-unit>";
            string blue = @"<lexical-unit>
                                <form lang='one'>
                                    <text>first</text>
                                </form>
                            </lexical-unit>";
            string ancestor = blue;

            CheckOneWay(blue, red, ancestor, "lexical-unit[ not(form)]");
        }

    }
}
