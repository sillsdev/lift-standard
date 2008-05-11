using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using LiftIO.Merging.XmlMerge;
using LiftIO.Parsing;

namespace LiftIO.Merging
{
    /// <summary>
    /// This is to be used be version control systems to do an intelligent 3-way merge of lift files
    /// </summary>
    public class LiftVersionControlMerger : IDisposable
    {
        private readonly string _ourLift;
        private readonly string _theirLift;
        private readonly string _ancestorLift;
        private readonly List<string> _processedIds = new List<string>();
        private readonly XmlDocument _ourDom;
        private readonly XmlDocument _theirDom;
        private readonly XmlDocument _ancestorDom;
        private IMergeStrategy _mergingStrategy;
        private StreamWriter _humanLog;

        public LiftVersionControlMerger(string ourLiftXml, string theirLiftXml, string ancestorLiftXml, IMergeStrategy mergeStrategy) 
        {
            _ourLift = ourLiftXml;
            _theirLift = theirLiftXml;
            _ancestorLift = ancestorLiftXml;
            _ourDom = new XmlDocument();
            _theirDom = new XmlDocument();
            _ancestorDom = new XmlDocument();

            _mergingStrategy = mergeStrategy;

         //   string humanLogPath = Path.Combine(Path.GetDirectoryName(ourLiftPath), Path.GetFileNameWithoutExtension(ourLiftPath)+ "_MergeLog.txt");
            
            //TODO:
            string humanLogPath = Path.GetTempFileName();
            _humanLog = System.IO.File.CreateText(humanLogPath);
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

            //REVIEW: this doesn't seem to take advantage of the most common situation,
            // which is an entry that *either* they changed, or we changed.  I expected to
            // see an early comparison against the ancestor to determine that.

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
                MergeResult result = _mergingStrategy.MakeMergedEntry(ourEntry, theirEntry, commonEntry);
                writer.WriteRaw(result.MergedNode.OuterXml);
                UpdateLogs(result);
            }
            _processedIds.Add(id);
        }

        private void UpdateLogs(MergeResult result)
        {
            foreach (IConflict conflict in result.Conflicts)
            {
                _humanLog.WriteLine(conflict.GetFullHumanReadableDescription());
            }
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

        #region IDisposable Members

        // TODO: implement full pattern, with checking

        public void Dispose()
        {
            _humanLog.Close();
            _humanLog.Dispose();
            _humanLog = null;
        }

        #endregion
    }
}