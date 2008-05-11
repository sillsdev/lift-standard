using System.Text;
using System.Xml;
using LiftIO.Merging.XmlMerge;

namespace LiftIO.Merging
{
    /// <summary>
    /// This strategy doesn't even try to put the entries together.  It just returns ours.
    /// </summary>
    public class DropTheirsMergeStrategy : IMergeStrategy
    {
        public MergeResult MakeMergedEntry(XmlNode ourEntry, XmlNode theirEntry, XmlNode unusedCommonEntry)
        {
            MergeResult r = new MergeResult();
            r.MergedNode = ourEntry;
            return r;
        }


    }
}