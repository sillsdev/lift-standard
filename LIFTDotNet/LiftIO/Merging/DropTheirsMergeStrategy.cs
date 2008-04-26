using System.Text;
using System.Xml;

namespace LiftIO.Merging
{
    /// <summary>
    /// This strategy doesn't even try to put the entries together.  It just returns ours.
    /// </summary>
    public class DropTheirsMergeStrategy : IMergeStrategy
    {
        public string MakeMergedEntry(XmlNode ourEntry, XmlNode theirEntry, XmlNode unusedCommonEntry)
        {
            return ourEntry.OuterXml;
        }


    }
}