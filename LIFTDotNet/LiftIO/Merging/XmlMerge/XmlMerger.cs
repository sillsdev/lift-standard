using System.Collections.Generic;
using System.Xml;
using LiftIO.Merging.XmlMerge;

namespace LiftIO.Tests.Merging
{
    public class XmlMerger
    {
//        public Dictionary<string, IFindNodeToMerge> _finders = new Dictionary<string, IFindNodeToMerge>();
        public Dictionary<string, ElementStrategy> _elementStrategies = new Dictionary<string, ElementStrategy>();

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
                        //log conflict
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
                    //log conflict we both changed it to different things
                }
            }

            // deal with their deletions
            foreach (XmlAttribute ourAttr in GetAttrs(ours))
            {

                XmlAttribute theirAttr = theirs.Attributes.GetNamedItem(ourAttr.Name) as XmlAttribute;
                XmlAttribute ancestorAttr = ancestor.Attributes.GetNamedItem(ourAttr.Name) as XmlAttribute;

                if (theirAttr == null && ancestorAttr != null)
                {
                    if (ourAttr.Value == ancestorAttr.Value)
                    {
                        ours.Attributes.Remove(ourAttr);
                    }
                    else
                    {
                        //todo log conflict
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

                IFindNodeToMerge finder = GetMergePartnerFinder(theirChildElement);
                XmlNode ourElement = finder.GetNodeToMerge(theirChildElement, ours);
                XmlNode ancestorElement = finder.GetNodeToMerge(theirChildElement, ancestor);

                if (ourElement == null)
                {
                    if (ancestorElement == null)
                    {
                        ours.AppendChild(theirChildElement);
                    }
                    else if (Utilities.AreXmlElementsEqual(ancestorElement, theirChildElement))
                    {
                        continue; // we deleted it, they didn't touch it
                    }
                    else //we deleted it, but at the same time, they changed it
                    {
                        //todo: should we add what they modified?
                        //needs a test first

                        //until then, this is a conflict  <-- todo

                        continue;
                    }
                }
                else if (Utilities.AreXmlElementsEqual(ourElement, ancestorElement))
                {
                    if (Utilities.AreXmlElementsEqual(ourElement, theirChildElement))
                    {
                        //nothing to do
                        continue;
                    }
                    else //theirs is new
                    {
                        Merge(ourElement, theirChildElement, ancestorElement);
                        //todo: need to recurse now?
                        //ourElement.ParentNode.ReplaceChild(theirElement, ourElement);
                    }
                }
                else
                { 
                    //TODO: are they mergeable? TODO: where are text nodes handled?

                    Merge(ourElement, theirChildElement, ancestorElement);
                    //log conflict
                }
            }

            // deal with their deletions
            foreach (XmlNode ourChildElement in ours.ChildNodes)
            {
                if (ourChildElement.NodeType != XmlNodeType.Element)
                {
                    continue;
                }
                IFindNodeToMerge finder = GetMergePartnerFinder(ourChildElement);
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
                        //todo log conflict
                    }
                }
            }

            return ours;
        }

        private IFindNodeToMerge GetMergePartnerFinder(XmlNode element)
        {
            ElementStrategy strategy;
            if (!this._elementStrategies.TryGetValue(element.Name, out strategy))
            {
                return new FindByEqualityOfTree();
            }
            return strategy._mergePartnerFinder;
        }

        private IDifferenceReportMaker GetDifferenceReportMaker(XmlNode element)
        {
            ElementStrategy strategy;
            if (!this._elementStrategies.TryGetValue(element.Name, out strategy))
            {
                return new DefaultDifferenceReportMaker();
            }
            return strategy._differenceReportMaker;
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
