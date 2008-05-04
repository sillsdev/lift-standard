using System.Collections.Generic;
using System.Xml;
using LiftIO.Merging.XmlMerge;

namespace LiftIO.Tests.Merging
{
    public class XmlMerger
    {
        public IMergeLogger _logger;
        public MergeStrategies _mergeStrategies;

        public XmlMerger(IMergeLogger logger)
        {
            _logger = logger;
            _mergeStrategies = new MergeStrategies();
        }

        private static List<XmlAttribute> GetAttrs(XmlNode node)
        {
            //need to copy so we can iterate while changing
            List<XmlAttribute> attrs = new List<XmlAttribute>();
            foreach (XmlAttribute attr in node.Attributes)
            {
                attrs.Add(attr);
            }
            return attrs;
        }

        public void MergeAttributes(ref XmlNode ours, XmlNode theirs, XmlNode ancestor)
        {
            foreach (XmlAttribute theirAttr in GetAttrs(theirs))
            {
                XmlAttribute ourAttr = ours.Attributes.GetNamedItem(theirAttr.Name) as XmlAttribute;
                XmlAttribute ancestorAttr = ancestor.Attributes.GetNamedItem(theirAttr.Name) as XmlAttribute;

                if (ourAttr == null)
                {
                    if (ancestorAttr == null)
                    {
                        ours.Attributes.Append(theirAttr);
                    }
                    else if (ancestorAttr.Value == theirAttr.Value)
                    {
                        continue; // we deleted it, they didn't touch it
                    }
                    else //we deleted it, but at the same time, they changed it
                    {
                        //todo: should we add what they modified?
                        //needs a test first

                        //until then, this is a conflict  <-- todo
                        _logger.RegisterConflict(new RemovedVsEditedAttributeConflict(theirAttr.Name, null, theirAttr.Value, ancestorAttr.Value));
                        continue;
                    }
                }
                else if (ancestorAttr == null) // we both introduced this attribute
                {
                    if (ourAttr.Value == theirAttr.Value)
                    {
                        //nothing to do
                        continue;
                    }
                    else 
                    {
                        _logger.RegisterConflict(new BothEdittedAttributeConflict(theirAttr.Name, ourAttr.Value, theirAttr.Value, null));
                    }
                }
                else if (ancestorAttr.Value == ourAttr.Value)
                {
                    if (ourAttr.Value == theirAttr.Value)
                    {
                        //nothing to do
                        continue;
                    }
                    else //theirs is a change
                    {
                        ourAttr.Value = theirAttr.Value;
                    }
                }
                else
                {
                    _logger.RegisterConflict(new BothEdittedAttributeConflict(theirAttr.Name, ourAttr.Value, theirAttr.Value, ancestorAttr.Value));
                }
            }

            // deal with their deletions
            foreach (XmlAttribute ourAttr in GetAttrs(ours))
            {

                XmlAttribute theirAttr = theirs.Attributes.GetNamedItem(ourAttr.Name) as XmlAttribute;
                XmlAttribute ancestorAttr = ancestor.Attributes.GetNamedItem(ourAttr.Name) as XmlAttribute;

                if (theirAttr == null && ancestorAttr != null)
                {
                    if (ourAttr.Value == ancestorAttr.Value) //we didn't change it, they deleted it
                    {
                        ours.Attributes.Remove(ourAttr);
                    }
                    else
                    {
                        _logger.RegisterConflict(new RemovedVsEditedAttributeConflict(ourAttr.Name, ourAttr.Value, null, ancestorAttr.Value));
                    }
                }
            }
        }

        public  XmlNode MergeElements(XmlNode ours, XmlNode theirs, XmlNode ancestor)
        {
            foreach (XmlNode theirChildElement in theirs.ChildNodes)
            {

                if (theirChildElement.NodeType != XmlNodeType.Element)
                {
                    continue;
                }

                IFindNodeToMerge finder = _mergeStrategies.GetMergePartnerFinder(theirChildElement);
                XmlNode ourChildElement = finder.GetNodeToMerge(theirChildElement, ours);
                XmlNode ancestorChildElement = finder.GetNodeToMerge(theirChildElement, ancestor);

                if (ourChildElement == null)
                {
                    if (ancestorChildElement == null)
                    {
                        ours.AppendChild(theirChildElement);
                    }
                    else if (Utilities.AreXmlElementsEqual(ancestorChildElement, theirChildElement))
                    {
                        continue; // we deleted it, they didn't touch it
                    }
                    else //we deleted it, but at the same time, they changed it
                    {
                        _logger.RegisterConflict(new RemovedVsEditedElementConflict(theirChildElement.Name, null, theirChildElement, ancestorChildElement));
                        continue;
                    }
                }
                else if (Utilities.AreXmlElementsEqual(ourChildElement, ancestorChildElement))
                {
                    if (Utilities.AreXmlElementsEqual(ourChildElement, theirChildElement))
                    {
                        //nothing to do
                        continue;
                    }
                    else //theirs is new
                    {
                        Merge(ourChildElement, theirChildElement, ancestorChildElement);
                    }
                }
                else
                { 
                    //TODO: are they mergeable? TODO: where are text nodes handled?

                    Merge(ourChildElement, theirChildElement, ancestorChildElement);
                }
            }

            // deal with their deletions
            foreach (XmlNode ourChildElement in ours.ChildNodes)
            {
                if (ourChildElement.NodeType != XmlNodeType.Element)
                {
                    continue;
                }
                IFindNodeToMerge finder = _mergeStrategies.GetMergePartnerFinder(ourChildElement);
                XmlNode theirChildElement = finder.GetNodeToMerge(ourChildElement, theirs);
                XmlNode ancestorChildElement = finder.GetNodeToMerge(ourChildElement, ancestor);

                if (theirChildElement == null && ancestorChildElement != null)
                {
                    if (Utilities.AreXmlElementsEqual(ourChildElement, ancestorChildElement))
                    {
                        ours.RemoveChild(ourChildElement);
                    }
                    else
                    {
                        _logger.RegisterConflict(new RemovedVsEditedElementConflict(ourChildElement.Name, ourChildElement, null, ancestorChildElement));
                    }
                }
            }

            return ours;
        }


        public XmlNode Merge(XmlNode ours, XmlNode theirs, XmlNode ancestor)
        {
            MergeAttributes(ref ours, theirs, ancestor);
            return MergeElements(ours,
                                 theirs,
                                 ancestor);
        }

        public string Merge(string ours, string theirs, string ancestor)
        {
            XmlDocument doc = new XmlDocument();
            XmlNode ourNode = Utilities.GetDocumentNodeFromRawXml(ours, doc);
            XmlNode theirNode = Utilities.GetDocumentNodeFromRawXml(theirs, doc);
            XmlNode ancestorNode = Utilities.GetDocumentNodeFromRawXml(ancestor, doc);

            return Merge(ourNode,theirNode,ancestorNode).OuterXml;
        }
    }


}
