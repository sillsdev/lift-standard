using System.Xml;

namespace LiftIO.Merging
{
    public interface IMergeStrategy
    {
        string MakeMergedEntry(XmlNode entry, XmlNode theirEntry, XmlNode commonEntry);
    }
}