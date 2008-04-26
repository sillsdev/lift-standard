using System;
using System.Collections.Generic;
using System.Text;
using LiftIO.Merging;
using NUnit.Framework;

namespace LiftIO.Tests.Merging
{
    [TestFixture]
    public class LiftSavvyMergingStrategyTests
    {
        [Test, Ignore("not yet")]
        public void OursDidNotChange_TheyHaveAddedLexemeFormAlternative_GetsAdded()
        {
            string ours = @"<?xml version='1.0' encoding='utf-8'?>
                    <lift version='0.10' producer='WeSay 1.0.0.0'>
                        <entry id='test'>
                            <lexical-unit>
                                <form lang='one'>
                                    <text>first</text>
                                </form>
                            </lexical-unit>
                        </entry>
                    </lift>";

            string theirs = @"<?xml version='1.0' encoding='utf-8'?>
                    <lift version='0.10' producer='WeSay 1.0.0.0'>
                        <entry id='test'>
                            <lexical-unit>
                                <form lang='one'>
                                    <text>first</text>
                                </form>
                                <form lang='two'>
                                    <text>second</text>
                                </form>
                            </lexical-unit>
                        </entry>
                    </lift>"; 
            string ancestor = ours;
            LiftVersionControlMerger merger = new LiftVersionControlMerger(ours, theirs, ancestor, new LiftSavvyMergeStrategy());
            string result = merger.GetMergedLift();
            XmlTestHelper.AssertXPathMatchesExactlyOne(result, "lift/entry/lexical-unit/form[@lang='one']/text[text()='first']");
            XmlTestHelper.AssertXPathMatchesExactlyOne(result, "lift/entry/lexical-unit/form[@lang='two']/text[text()='second']");
        }

        [Test]
        public void EachHasNewSense_BothSensesCoveyed()
        {
            string ours = @"<?xml version='1.0' encoding='utf-8'?>
                    <lift version='0.10' producer='WeSay 1.0.0.0'>
                        <entry id='test'>
                            <sense id='123'>
                                 <gloss lang='a'>
                                    <text>ourSense</text>
                                 </gloss>
                             </sense>
                        </entry>
                    </lift>";

            string theirs = @"<?xml version='1.0' encoding='utf-8'?>
                    <lift version='0.10' producer='WeSay 1.0.0.0'>
                        <entry id='test'>
                            <sense id='456'>
                                 <gloss lang='a'>
                                    <text>theirSense</text>
                                 </gloss>
                             </sense>
                        </entry>
                    </lift>";
            string ancestor = @"<?xml version='1.0' encoding='utf-8'?>
                    <lift version='0.10' producer='WeSay 1.0.0.0'>
                        <entry id='test'/>
                    </lift>";
            LiftVersionControlMerger merger = new LiftVersionControlMerger(ours, theirs, ancestor, new LiftSavvyMergeStrategy());
            string result = merger.GetMergedLift();
            XmlTestHelper.AssertXPathMatchesExactlyOne(result, "lift/entry[@id='test']");
            XmlTestHelper.AssertXPathMatchesExactlyOne(result, "lift/entry[@id='test' and sense[@id='123']/gloss/text='ourSense']");
            XmlTestHelper.AssertXPathMatchesExactlyOne(result, "lift/entry[@id='test' and sense[@id='456']/gloss/text='theirSense']");
        }
    }
}
