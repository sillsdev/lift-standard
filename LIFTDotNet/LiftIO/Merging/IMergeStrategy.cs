using System.Xml;
using LiftIO.Merging.XmlMerge;

namespace LiftIO.Merging
{
    public interface IMergeStrategy
    {
        MergeResult MakeMergedEntry(XmlNode ourEntry, XmlNode theirEntry, XmlNode commonEntry);
    }
}