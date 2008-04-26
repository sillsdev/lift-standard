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
     

        [Test, Ignore("Not implemented")]
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
            LiftVersionControlMerger merger = new LiftVersionControlMerger(ours, theirs, ancestor, new PoorMansMergeStrategy());
            string result = merger.GetMergedLift();
            XmlTestHelper.AssertXPathMatchesExactlyOne(result, "lift/entry[@id='test']");
            XmlTestHelper.AssertXPathMatchesExactlyOne(result, "lift/entry[@id='test' and sense[@id='123']/gloss/text='ourSense']");
            XmlTestHelper.AssertXPathMatchesExactlyOne(result, "lift/entry[@id='test' and sense[@id='456']/gloss/text='theirSense']");
        }
    }
}
