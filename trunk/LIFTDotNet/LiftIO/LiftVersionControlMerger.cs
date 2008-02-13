using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace LiftIO
{
    public class LiftVersionControlMerger
    {
        private readonly string _ourLift;
        private readonly string _theirLift;
        private readonly string _commonAncestorLift;
        private List<string> _processedIds = new List<string>();
        private XmlDocument _ours;
        private XmlDocument _theirs;
        private XmlDocument _ancestor;

        public LiftVersionControlMerger(string ours, string theirs, string common)
        {
            _ourLift = ours;
            _theirLift = theirs;
            _commonAncestorLift = common;
            _ours = new XmlDocument();
            _theirs = new XmlDocument();
            _ancestor = new XmlDocument();
        }
        public string GetMergedLift()
        {
            _ours.LoadXml(_ourLift);
            _theirs.LoadXml(_theirLift);
            _ancestor.LoadXml(_commonAncestorLift);

            StringBuilder builder = new StringBuilder();
            using (XmlWriter writer = XmlWriter.Create(builder))
            {
                WriteStartOfLiftElement( writer);
                foreach (XmlNode e in _ours.SelectNodes("lift/entry"))
                {
                    ProcessEntry(writer, e);
                }

                //now process any remaining elements in "theirs"
                foreach (XmlNode e in _theirs.SelectNodes("lift/entry"))
                {
                    string id = GetId(e);
                    if (!_processedIds.Contains(id))
                    {
                        ProcessEntryWeKnowDoesntNeedMerging(e, id, writer);
                    }
                } 
                writer.WriteEndElement();
                
            }
            return builder.ToString();
        }

        private XmlNode FindEntry(XmlDocument doc, string id)
        {
            return doc.SelectSingleNode("lift/entry[@id='"+id+"']");
        }

        private void ProcessEntry(XmlWriter writer, XmlNode ourEntry)
        {
            string id = GetId(ourEntry);
            XmlNode theirEntry = FindEntry(_theirs, id);
            if (theirEntry == null)
            {
                ProcessEntryWeKnowDoesntNeedMerging(ourEntry, id, writer);
            }
            else if (AreTheSame(ourEntry, theirEntry))
            {
                writer.WriteRaw(ourEntry.OuterXml);
            }
            else
            {
                writer.WriteRaw(MakeMergedEntry(ourEntry, theirEntry));
            }
            _processedIds.Add(id);
        }

        private void ProcessEntryWeKnowDoesntNeedMerging(XmlNode entry, string id, XmlWriter writer)
        {
            if(FindEntry(_ancestor,id) ==null)
            {
                writer.WriteRaw(entry.OuterXml); //it's new
            }
            else
            {
                // it must have been deleted by the other guy
            }
        }

        private string MakeMergedEntry(XmlNode ourEntry, XmlNode theirEntry)
        {
            XmlNode mergeNoteFieldNode = ourEntry.OwnerDocument.CreateElement("field");
            AddAttribute(mergeNoteFieldNode, "tag", "mergeConflict");
            AddDateCreatedAttribute(mergeNoteFieldNode);
            StringBuilder b = new StringBuilder();
            b.Append("<trait name='looserData'>");
            b.AppendFormat("<![CDATA[{0}]]>", theirEntry.OuterXml);
            b.Append("</trait>");
            mergeNoteFieldNode.InnerXml = b.ToString();
            ourEntry.AppendChild(mergeNoteFieldNode);
            return ourEntry.OuterXml;
        }

        private void AddDateCreatedAttribute(XmlNode elementNode)
        {
            AddAttribute(elementNode, "dateCreated", DateTime.Now.ToString(Extensible.LiftTimeFormatNoTimeZone));
        }

        private void AddAttribute(XmlNode element, string name, string value)
        {
            XmlAttribute attr = element.OwnerDocument.CreateAttribute(name);
            attr.Value = value;
            element.Attributes.Append(attr);
        }

        private bool AreTheSame(XmlNode ourEntry, XmlNode theirEntry)
        {
            //for now...
            if (GetModifiedDate(theirEntry) == GetModifiedDate(ourEntry) 
                && !(GetModifiedDate(theirEntry) == default(DateTime)))
                return true;

            if(ourEntry.OuterXml == theirEntry.OuterXml) // it'd be nice to have something tolerant for this
                return true;

            return false;
        }

        private DateTime GetModifiedDate(XmlNode entry)
        {
            XmlAttribute d = entry.Attributes["dateModified"];
            if (d == null)
                return default(DateTime); //review
            return DateTime.Parse(d.Value);
        }

        private void WriteStartOfLiftElement(XmlWriter writer)
        {
            XmlNode liftNode = _ours.SelectSingleNode("lift");

            writer.WriteStartElement(liftNode.Name);
            foreach (XmlAttribute attribute in liftNode.Attributes)
            {
                writer.WriteAttributeString(attribute.Name, attribute.Value);
            }
        }

        private string GetId(XmlNode e)
        {
            return e.Attributes["id"].Value;
        }
    }
}
