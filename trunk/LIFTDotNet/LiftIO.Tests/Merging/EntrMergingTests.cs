using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using LiftIO.Merging;
using NUnit.Framework;

namespace LiftIO.Tests.Merging
{
    [TestFixture]
    public class EntryMergingTests
    {
        [Test]
        public void EachEditsSameFormOfLexicalUnit_GetOursAndConflict()
        {
            string ours = @"<?xml version='1.0' encoding='utf-8'?>
                    <lift version='0.12' >
                        <entry id='test'>
                            <lexical-unit>
                                <form lang='one'><text>ours</text></form>
                            </lexical-unit>
                        </entry>
                    </lift>";

            string theirs = @"<?xml version='1.0' encoding='utf-8'?>
                    <lift version='0.12' >
                        <entry id='test'>
                            <lexical-unit>
                                <form lang='one'><text>theirs</text></form>
                            </lexical-unit>
                        </entry>
                    </lift>";
            string ancestor = @"<?xml version='1.0' encoding='utf-8'?>
                    <lift version='0.12'>
                        <entry id='test'>
                            <lexical-unit>
                                <form lang='one'><text>original</text></form>
                            </lexical-unit>
                        </entry>
                    </lift>";
            LiftVersionControlMerger merger = new LiftVersionControlMerger(ours, theirs, ancestor, new EntryMerger());
            string result = merger.GetMergedLift();
            XmlTestHelper.AssertXPathMatchesExactlyOne(result, "lift/entry[count(lexical-unit) = 1]");
            XmlTestHelper.AssertXPathMatchesExactlyOne(result, "lift/entry/lexical-unit/form/text[text()='ours']");

            //todo assert conflict
        }

        [Test]
        public void EachEditsSameFormOfCitationForm_GetOursAndConflict()
        {
            string ours = @"<?xml version='1.0' encoding='utf-8'?>
                    <lift version='0.12' >
                        <entry id='test'>
                            <citation>
                                <form lang='one'><text>ours</text></form>
                            </citation>
                        </entry>
                    </lift>";

            string theirs = @"<?xml version='1.0' encoding='utf-8'?>
                    <lift version='0.12' >
                        <entry id='test'>
                            <citation>
                                <form lang='one'><text>theirs</text></form>
                            </citation>
                        </entry>
                    </lift>";
            string ancestor = @"<?xml version='1.0' encoding='utf-8'?>
                    <lift version='0.12'>
                        <entry id='test'>
                            <citation>
                                <form lang='one'><text>original</text></form>
                            </citation>
                        </entry>
                    </lift>";
            LiftVersionControlMerger merger = new LiftVersionControlMerger(ours, theirs, ancestor, new EntryMerger());
            string result = merger.GetMergedLift();
            XmlTestHelper.AssertXPathMatchesExactlyOne(result, "lift/entry[count(citation) = 1]");
            XmlTestHelper.AssertXPathMatchesExactlyOne(result, "lift/entry/citation/form/text[text()='ours']");

            //todo assert conflict
        }

        [Test]
        public void EachEditsSameFormOfField_GetOursAndConflict()
        {
            string ours = @"<?xml version='1.0' encoding='utf-8'?>
                    <lift version='0.12' >
                        <entry id='test'>
                            <field type='1'>
                                <form lang='one'><text>ours</text></form>
                            </field>
                        </entry>
                    </lift>";

            string theirs = @"<?xml version='1.0' encoding='utf-8'?>
                    <lift version='0.12' >
                        <entry id='test'>
                            <field type='1'>
                                <form lang='one'><text>theirs</text></form>
                            </field>                        </entry>
                    </lift>";
            string ancestor = @"<?xml version='1.0' encoding='utf-8'?>
                    <lift version='0.12'>
                        <entry id='test'>
                            <field type='1'>
                                <form lang='one'><text>common</text></form>
                            </field>
                        </entry>
                    </lift>";
            LiftVersionControlMerger merger = new LiftVersionControlMerger(ours, theirs, ancestor, new EntryMerger());
            string result = merger.GetMergedLift();
            XmlTestHelper.AssertXPathMatchesExactlyOne(result, "lift/entry[count(field) = 1]");
            XmlTestHelper.AssertXPathMatchesExactlyOne(result, "lift/entry/field[@type='1']/form/text[text()='ours']");

            //todo assert conflict
        }

        [Test]
        public void EachEditsSameFormOfFieldAndAddsForm_GetCorrectMerge()
        {
            string ours = @"<?xml version='1.0' encoding='utf-8'?>
                    <lift version='0.12' >
                        <entry id='test'>
                            <field type='0'>
                                <form lang='one'><text>our0</text></form>
                            </field>
                            <field type='1'>
                                <form lang='one'><text>ours</text></form>
                            </field>
                        </entry>
                    </lift>";

            string theirs = @"<?xml version='1.0' encoding='utf-8'?>
                    <lift version='0.12' >
                        <entry id='test'>
                            <field type='1'>
                                <form lang='one'><text>theirs</text></form>
                            </field>                        
                            <field type='2'>
                                <form lang='one'><text>their2</text></form>
                            </field>

                        </entry>
                    </lift>";
            string ancestor = @"<?xml version='1.0' encoding='utf-8'?>
                    <lift version='0.12'>
                        <entry id='test'>
                            <field type='1'>
                                <form lang='one'><text>common</text></form>
                            </field>
                        </entry>
                    </lift>";
            LiftVersionControlMerger merger = new LiftVersionControlMerger(ours, theirs, ancestor, new EntryMerger());
            string result = merger.GetMergedLift();
            XmlTestHelper.AssertXPathMatchesExactlyOne(result, "lift/entry[count(field) = 3]");
            XmlTestHelper.AssertXPathMatchesExactlyOne(result, "lift/entry/field[@type='0']/form/text[text()='our0']");
            XmlTestHelper.AssertXPathMatchesExactlyOne(result, "lift/entry/field[@type='1']/form/text[text()='ours']");
            XmlTestHelper.AssertXPathMatchesExactlyOne(result, "lift/entry/field[@type='2']/form/text[text()='their2']");

            //todo assert conflict
        }
    }
}
