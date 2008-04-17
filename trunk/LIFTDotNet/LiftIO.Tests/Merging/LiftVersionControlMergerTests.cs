using System;
using System.Xml;
using LiftIO.Merging;
using NUnit.Framework;

namespace LiftIO.Tests.Merging
{
    [TestFixture]
    public class LiftVersionControlMergerTests
    {
        private string _ours;
        private string _theirs;
        private string _ancestor;


        [SetUp]
        public void Setup()
        {
            this._ours = @"<?xml version='1.0' encoding='utf-8'?>
                    <lift version='0.10' producer='WeSay 1.0.0.0'>
                        <entry id='usOnly'/>
                        <entry id='sameInBoth'>
                            <lexical-unit>
                                <form lang='a'>
                                    <text>form a</text>
                                </form>
                            </lexical-unit>
                         </entry>
                        <entry id='doomedByOther'/>
                    </lift>";

            this._theirs = @"<?xml version='1.0' encoding='utf-8'?>
                    <lift version='0.10' producer='WeSay 1.0.0.0'>
                       <entry id='sameInBoth'>
                            <lexical-unit>
                                <form lang='b'>
                                    <text>form b</text>
                                </form>
                            </lexical-unit>
                         </entry>
                        <entry id='themOnly'>
                            <lexical-unit>
                                <form lang='b'>
                                    <text>form b</text>
                                </form>
                            </lexical-unit>
                         </entry>
                        <entry id='doomedByUs'/>

                        <entry id='newSensesCollision'>
                            <sense>
                                 <gloss lang='a'>
                                    <text></text>
                                 </gloss>
                             </sense>
                        </entry>

                    </lift>";
            this._ancestor = @"<?xml version='1.0' encoding='utf-8'?>
                    <lift version='0.10' producer='WeSay 1.0.0.0'>
                        <entry id='doomedByOther'/>
                        <entry id='doomedByUs'/>
                        <entry id='newSensesCollision'>
                            <sense>
                                 <gloss lang='a'>
                                    <text></text>
                                 </gloss>
                             </sense>
                        </entry>
                    </lift>";
        }

        [Test]
        public void Conflict_TheirsAppearsInCollisionNote()
        {
            this._ours = @"<?xml version='1.0' encoding='utf-8'?>
                    <lift version='0.10' producer='WeSay 1.0.0.0'>
                        <entry id='lexicalformcollission'>
                            <lexical-unit>
                                <form lang='x'>
                                    <text>ours</text>
                                </form>
                            </lexical-unit>
                        </entry>
                    </lift>";

            this._theirs = @"<?xml version='1.0' encoding='utf-8'?>
                    <lift version='0.10' producer='WeSay 1.0.0.0'>
                        <entry id='lexicalformcollission'>
                            <lexical-unit>
                                <form lang='x'>
                                    <text>theirs</text>
                                </form>
                            </lexical-unit>
                        </entry>
                    </lift>";
            this._ancestor = @"<?xml version='1.0' encoding='utf-8'?>
                    <lift version='0.10' producer='WeSay 1.0.0.0'>
                        <entry id='lexicalformcollission'/>
                    </lift>";
            LiftVersionControlMerger merger = new LiftVersionControlMerger(this._ours, this._theirs, this._ancestor);
            string result = merger.GetMergedLift();
            AssertXPathMatchesExactlyOne(result, "lift/entry[@id='lexicalformcollission']");
            AssertXPathMatchesExactlyOne(result, "lift/entry");//just one
            AssertXPathMatchesExactlyOne(result, "lift/entry/field[@tag='mergeConflict']/trait[@name = 'looserData']");
            AssertXPathMatchesExactlyOne(result, "lift/entry/field[@tag='mergeConflict' and @dateCreated]");

        }

        [Test, Ignore("Not implemented")]
        public void EachHasNewSense_BothSensesCoveyed()
        {
            this._ours = @"<?xml version='1.0' encoding='utf-8'?>
                    <lift version='0.10' producer='WeSay 1.0.0.0'>
                        <entry id='newSensesCollision'>
                            <sense>
                                 <gloss lang='a'>
                                    <text>ourSense</text>
                                 </gloss>
                             </sense>
                        </entry>
                    </lift>";

            this._theirs = @"<?xml version='1.0' encoding='utf-8'?>
                    <lift version='0.10' producer='WeSay 1.0.0.0'>
                        <entry id='newSensesCollision'>
                            <sense>
                                 <gloss lang='a'>
                                    <text>theirSense</text>
                                 </gloss>
                             </sense>
                        </entry>
                    </lift>";
            this._ancestor = @"<?xml version='1.0' encoding='utf-8'?>
                    <lift version='0.10' producer='WeSay 1.0.0.0'>
                        <entry id='newSensesCollision'/>
                    </lift>"; 
            LiftVersionControlMerger merger = new LiftVersionControlMerger(this._ours, this._theirs, this._ancestor);
            string result = merger.GetMergedLift();
            AssertXPathMatchesExactlyOne(result, "lift/entry[@id='newSensesCollision']");
            AssertXPathMatchesExactlyOne(result, "lift/entry[@id='newSensesCollision' and sense/gloss/text='ourSense']");
            AssertXPathMatchesExactlyOne(result, "lift/entry[@id='newSensesCollision' and sense/gloss/text='theirSense']");
        }

        [Test]
        public void NewEntryFromUs_Conveyed()
        {
            LiftVersionControlMerger merger = new LiftVersionControlMerger(this._ours, this._theirs, this._ancestor);
            string result = merger.GetMergedLift();
            AssertXPathMatchesExactlyOne(result, "lift/entry[@id='usOnly']");
        }

        [Test]
        public void NewEntryFromThem_Conveyed()
        { 
            LiftVersionControlMerger merger = new LiftVersionControlMerger(this._ours, this._theirs, this._ancestor);
            string result = merger.GetMergedLift();
            AssertXPathMatchesExactlyOne(result, "lift/entry[@id='themOnly']");
        }
        [Test]
        public void UnchangedEntryInBoth_NotDuplicated()
        {
            LiftVersionControlMerger merger = new LiftVersionControlMerger(this._ours, this._theirs, this._ancestor);
            string result = merger.GetMergedLift();
            AssertXPathMatchesExactlyOne(result, "lift/entry[@id='sameInBoth']");
        }
        [Test]
        public void EntryRemovedByOther_Removed()
        {
            LiftVersionControlMerger merger = new LiftVersionControlMerger(this._ours, this._theirs, this._ancestor);
            string result = merger.GetMergedLift();
            AssertXPathMatchesExactlyOne(result, "lift[not(entry/@id='doomedByOther')]");
        }

        [Test]
        public void EntryRemovedByUs_Removed()
        {
            LiftVersionControlMerger merger = new LiftVersionControlMerger(this._ours, this._theirs, this._ancestor);
            string result = merger.GetMergedLift();
            AssertXPathMatchesExactlyOne(result, "lift[not(entry/@id='doomedByUs')]");
        }

        [Test, Ignore("Not implemented")]
        public void ReorderedEntry_Reordered()
        {
        }
        [Test, Ignore("Not implemented")]
        public void OlderLiftVersion_Handled()
        {//what to do?
        }

        [Test, Ignore("Not implemented")]
        public void NewerLiftVersion_Handled()
        {//what to do?
        }

        [Test, Ignore("Not implemented")]
        public void MetaData_Preserved()
        {
        }

        [Test, Ignore("Not implemented")]
        public void MetaData_Merged()
        {
        }
        
        private static void AssertXPathMatchesExactlyOne(string xml, string xpath)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);
            XmlNodeList nodes = doc.SelectNodes(xpath);
            if (nodes == null || nodes.Count != 1)
            {
                XmlWriterSettings settings = new XmlWriterSettings();
                settings.Indent = true;
                settings.ConformanceLevel = ConformanceLevel.Fragment;
                XmlWriter writer = XmlTextWriter.Create(Console.Out, settings);
                doc.WriteContentTo(writer);
                writer.Flush();
                if (nodes !=null && nodes.Count > 1)
                {
                    Assert.Fail("Too Many: XPath failed: {0}", xpath);
                }
                else
                {
                    Assert.Fail("No Match: XPath failed: {0}", xpath);
                }
            }
        }
    }
}