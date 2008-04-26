using System;
using System.Collections.Generic;
using System.Text;
using LiftIO.Merging;
using NUnit.Framework;

namespace LiftIO.Tests.Merging
{
    public class PoorMansMergingStrategyTests
    {
        [Test]
        public void Conflict_TheirsAppearsInCollisionNote()
        {
            string ours = @"<?xml version='1.0' encoding='utf-8'?>
                    <lift version='0.10' producer='WeSay 1.0.0.0'>
                        <entry id='lexicalformcollission'>
                            <lexical-unit>
                                <form lang='x'>
                                    <text>ours</text>
                                </form>
                            </lexical-unit>
                        </entry>
                    </lift>";

            string theirs = @"<?xml version='1.0' encoding='utf-8'?>
                    <lift version='0.10' producer='WeSay 1.0.0.0'>
                        <entry id='lexicalformcollission'>
                            <lexical-unit>
                                <form lang='x'>
                                    <text>theirs</text>
                                </form>
                            </lexical-unit>
                        </entry>
                    </lift>";
            string ancestor = @"<?xml version='1.0' encoding='utf-8'?>
                    <lift version='0.10' producer='WeSay 1.0.0.0'>
                        <entry id='lexicalformcollission'/>
                    </lift>";
            LiftVersionControlMerger merger = new LiftVersionControlMerger(ours, theirs, ancestor, new PoorMansMergeStrategy());
            string result = merger.GetMergedLift();
            XmlTestHelper.AssertXPathMatchesExactlyOne(result, "lift/entry[@id='lexicalformcollission']");
            XmlTestHelper.AssertXPathMatchesExactlyOne(result, "lift/entry");//just one
            XmlTestHelper.AssertXPathMatchesExactlyOne(result, "lift/entry/field[@type='mergeConflict']/trait[@name = 'looserData']");
            XmlTestHelper.AssertXPathMatchesExactlyOne(result, "lift/entry/field[@type='mergeConflict' and @dateCreated]");

        }

        [Test, Ignore("Not implemented")]
        public void EachHasNewSense_BothSensesCoveyed()
        {
            string ours = @"<?xml version='1.0' encoding='utf-8'?>
                    <lift version='0.10' producer='WeSay 1.0.0.0'>
                        <entry id='test'>
                            <sense>
                                 <gloss lang='a'>
                                    <text>ourSense</text>
                                 </gloss>
                             </sense>
                        </entry>
                    </lift>";

            string theirs = @"<?xml version='1.0' encoding='utf-8'?>
                    <lift version='0.10' producer='WeSay 1.0.0.0'>
                        <entry id='test'>
                            <sense>
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
            LiftVersionControlMerger merger = new LiftVersionControlMerger(ours, theirs, ancestor, new PoorMansMergeStrategy());
            string result = merger.GetMergedLift();
            XmlTestHelper.AssertXPathMatchesExactlyOne(result, "lift/entry[@id='test']");
            XmlTestHelper.AssertXPathMatchesExactlyOne(result, "lift/entry[@id='test' and sense/gloss/text='ourSense']");
            XmlTestHelper.AssertXPathMatchesExactlyOne(result, "lift/entry[@id='test' and sense/gloss/text='theirSense']");
        }
    }
}
