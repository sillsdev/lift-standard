using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using LiftIO.Parsing;

namespace LiftIO.Merging
{
    /// <summary>
    /// This is to be used be version control systems to do an intelligent 3-way merge of lift files
    /// </summary>
    public class LiftVersionControlMerger
    {
        private readonly string _ourLift;
        private readonly string _theirLift;
        private readonly string _ancestorLift;
        private readonly List<string> _processedIds = new List<string>();
        private readonly XmlDocument _ourDom;
        private readonly XmlDocument _theirDom;
        private readonly XmlDocument _ancestorDom;
        private IMergeStrategy _mergingStrategy;

        public LiftVersionControlMerger(string ourLiftPath, string theirLiftPath, string ancestorLiftPath, IMergeStrategy mergeStrategy)
        {
            _ourLift = ourLiftPath;
            _theirLift = theirLiftPath;
            _ancestorLift = ancestorLiftPath;
            _ourDom = new XmlDocument();
            _theirDom = new XmlDocument();
            _ancestorDom = new XmlDocument();

            _mergingStrategy = mergeStrategy;
        }

        public string GetMergedLift()
        {
            _ourDom.LoadXml(_ourLift);
            _theirDom.LoadXml(_theirLift);
            _ancestorDom.LoadXml(_ancestorLift);

            StringBuilder builder = new StringBuilder();
            using (XmlWriter writer = XmlWriter.Create(builder))
            {
                WriteStartOfLiftElement( writer);
                foreach (XmlNode e in _ourDom.SelectNodes("lift/entry"))
                {
                    ProcessEntry(writer, e);
                }

                //now process any remaining elements in "theirs"
                foreach (XmlNode e in _theirDom.SelectNodes("lift/entry"))
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

        private static XmlNode FindEntry(XmlNode doc, string id)
        {
            return doc.SelectSingleNode("lift/entry[@id='"+id+"']");
        }

        private void ProcessEntry(XmlWriter writer, XmlNode ourEntry)
        {
            string id = GetId(ourEntry);
            XmlNode theirEntry = FindEntry(_theirDom, id);
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
                XmlNode commonEntry = FindEntry(_ancestorDom, id);
                writer.WriteRaw(_mergingStrategy.MakeMergedEntry(ourEntry, theirEntry, commonEntry));
            }
            _processedIds.Add(id);
        }

        private void ProcessEntryWeKnowDoesntNeedMerging(XmlNode entry, string id, XmlWriter writer)
        {
            if(FindEntry(_ancestorDom,id) ==null)
            {
                writer.WriteRaw(entry.OuterXml); //it's new
            }
            else
            {
                // it must have been deleted by the other guy
            }
        }



        internal static void AddDateCreatedAttribute(XmlNode elementNode)
        {
            AddAttribute(elementNode, "dateCreated", DateTime.Now.ToString(Extensible.LiftTimeFormatNoTimeZone));
        }

        internal static void AddAttribute(XmlNode element, string name, string value)
        {
            XmlAttribute attr = element.OwnerDocument.CreateAttribute(name);
            attr.Value = value;
            element.Attributes.Append(attr);
        }

        private static bool AreTheSame(XmlNode ourEntry, XmlNode theirEntry)
        {
            //for now...
            if (GetModifiedDate(theirEntry) == GetModifiedDate(ourEntry) 
                && !(GetModifiedDate(theirEntry) == default(DateTime)))
                return true;

            return Utilities.AreXmlElementsEqual(ourEntry.OuterXml, theirEntry.OuterXml);
        }

        private static DateTime GetModifiedDate(XmlNode entry)
        {
            XmlAttribute d = entry.Attributes["dateModified"];
            if (d == null)
                return default(DateTime); //review
            return DateTime.Parse(d.Value);
        }

        private void WriteStartOfLiftElement(XmlWriter writer)
        {
            XmlNode liftNode = _ourDom.SelectSingleNode("lift");

            writer.WriteStartElement(liftNode.Name);
            foreach (XmlAttribute attribute in liftNode.Attributes)
            {
                writer.WriteAttributeString(attribute.Name, attribute.Value);
            }
        }

        private static string GetId(XmlNode e)
        {
            return e.Attributes["id"].Value;
        }
    }
}