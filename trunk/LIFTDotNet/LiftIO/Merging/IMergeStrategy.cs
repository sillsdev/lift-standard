using System.Xml;

namespace LiftIO.Merging
{
    public interface IMergeStrategy
    {
        string MakeMergedEntry(XmlNode ourEntry, XmlNode theirEntry, XmlNode commonEntry);
    }
}