using System.Text;
using System.Xml;
using LiftIO.Merging.XmlMerge;

namespace LiftIO.Merging
{
    /// <summary>
    /// This strategy doesn't even try to put the entries together.  It just takes "their" entry
    /// and sticks it in a merge failure field
    /// </summary>
    public class PoorMansMergeStrategy : IMergeStrategy
    {
        public MergeResult MakeMergedEntry(XmlNode ourEntry, XmlNode theirEntry, XmlNode unusedCommonEntry)
        {
            XmlNode mergeNoteFieldNode = ourEntry.OwnerDocument.CreateElement("field");
            LiftVersionControlMerger.AddAttribute(mergeNoteFieldNode, "type", "mergeConflict");
            LiftVersionControlMerger.AddDateCreatedAttribute(mergeNoteFieldNode);
            StringBuilder b = new StringBuilder();
            b.Append("<trait name='looserData'>");
            b.AppendFormat("<![CDATA[{0}]]>", theirEntry.OuterXml);
            b.Append("</trait>");
            mergeNoteFieldNode.InnerXml = b.ToString();
            ourEntry.AppendChild(mergeNoteFieldNode);
            MergeResult r = new MergeResult();
            r.MergedNode = ourEntry;
            return r;
        }
    }
}