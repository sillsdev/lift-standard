using System;
using LiftIO.Merging.XmlMerge;
using NUnit.Framework;

namespace LiftIO.Tests.Merging
{
    [TestFixture]
    public class XmlMerger_LeastCommonDenomiTests
    {



        private MergeResult CheckOneWay(string ours, string theirs, string ancestor, params string[] xpaths)
        {
            XmlMerger m = new XmlMerger();
            m._mergeStrategies._elementStrategies.Add("a", ElementStrategy.CreateForKeyedElement("key"));
            m._mergeStrategies._elementStrategies.Add("b", ElementStrategy.CreateForKeyedElement("key"));
            m._mergeStrategies._elementStrategies.Add("c", ElementStrategy.CreateForKeyedElement("key"));
            MergeResult result = m.Merge(ours, theirs, ancestor, false);
            foreach (string xpath in xpaths)
            {
                XmlTestHelper.AssertXPathMatchesExactlyOne(result.MergedNode, xpath);
            }
            return result;
        }

      

        [Test, Ignore("Not yet")]
        public void TextElement_EachEditted_KeepAncestor()
        {
            string ancestor = @"<t>original</t>";
            string ours = @"<t>mine</t>";
            string theirs = @"<t>theirs</t>";

            XmlMerger m = new XmlMerger();
            MergeResult result = m.Merge(ours, theirs, ancestor, false);
            XmlTestHelper.AssertXPathMatchesExactlyOne(result.MergedNode, "t[text()='original']");
        }

        [Test, Ignore("Not yet")]
        public void TextElement_WeEdittedTheyDeleted_KeepAncestor()
        {
            string ancestor = @"<t>original</t>";
            string ours = @"<t>mine</t>";
            string theirs = @"<t></t>";

            XmlMerger m = new XmlMerger();
            MergeResult result = m.Merge(ours, theirs, ancestor, false);
            XmlTestHelper.AssertXPathMatchesExactlyOne(result.MergedNode, "t[text()='original']");
        }
    }
}
