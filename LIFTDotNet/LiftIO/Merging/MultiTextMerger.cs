using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace LiftIO.Merging
{
    public class MultiTextMerger
    {
        internal static XmlNode MergeMultiTextPieces(string ours, string theirs, string ancestor)
        {
            return MergeMultiTextPieces(ours, theirs, ancestor, null);
        }


        internal static XmlNode MergeMultiTextPieces(string ours, string theirs, string ancestor, XmlNode optionalNodeMaker)
        {
            if (string.IsNullOrEmpty(ours) && string.IsNullOrEmpty(theirs))
            {
                return null;
            }

            if (optionalNodeMaker == null)
                optionalNodeMaker = new XmlDocument();

            if (string.IsNullOrEmpty(ours))
            {
                return Utilities.GetDocumentNodeFromRawXml(theirs, optionalNodeMaker);
            }

            if (string.IsNullOrEmpty(theirs))
            {
                return Utilities.GetDocumentNodeFromRawXml(ours, optionalNodeMaker);
            }


            return MergeMultiTextPieces(Utilities.GetDocumentNodeFromRawXml(ours, optionalNodeMaker),
                                         Utilities.GetDocumentNodeFromRawXml(theirs, optionalNodeMaker),
                                         Utilities.GetDocumentNodeFromRawXml(ancestor, optionalNodeMaker));
        }

        internal static XmlNode MergeMultiTextPieces(XmlNode ours, XmlNode theirs, XmlNode ancestor)
        {
            MergeAttributes(ref ours, theirs, ancestor);
            return MergeElements(ours, theirs, ancestor, "lang");
        }

        private static void MergeAttributes(ref XmlNode ours, XmlNode theirs, XmlNode ancestor)
        {
            
        }

        private static XmlNode MergeElements(XmlNode ours, XmlNode theirs, XmlNode ancestor, string keyAttribute)
        {
            foreach (XmlNode theirElement in theirs.ChildNodes)
            {
                if(theirElement.NodeType != XmlNodeType.Element)
                {
                    continue;
                }
                string key = Utilities.GetStringAttribute(theirElement, keyAttribute);
                string xpath = string.Format("{0}[@{1}='{2}']", theirElement.Name, keyAttribute, key);
                XmlNode ourElement = ours.SelectSingleNode(xpath);
                XmlNode ancestorElement = ancestor.SelectSingleNode(xpath);

                if (ourElement == null)
                {
                    if(ancestorElement == null)
                    {
                        ours.AppendChild(theirElement);
                    }
                    else if(Utilities.AreXmlElementsEqual(ancestorElement,theirElement))
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
                else if(Utilities.AreXmlElementsEqual(ourElement,ancestorElement) )
                {
                    if(Utilities.AreXmlElementsEqual(ourElement, theirElement))
                    {
                        //nothing to do
                        continue;
                    }
                    else //theirs is new
                    {
                        ourElement.ParentNode.ReplaceChild(theirElement, ourElement);
                    }
                }
                else
                {
                    //log conflict
                }
            }

            // deal with their deletions
            foreach (XmlNode ourElement in ours.ChildNodes)
            {
                if (ourElement.NodeType != XmlNodeType.Element)
                {
                    continue;
                }
                string key = Utilities.GetStringAttribute(ourElement, keyAttribute);
                XmlNode theirElement = theirs.SelectSingleNode("./form[@" + keyAttribute + "='" + key + "']");
                XmlNode ancestorElement = ancestor.SelectSingleNode("./form[@" + keyAttribute + "='" + key + "']");

                if (theirElement == null && ancestorElement != null)
                {
                    if (Utilities.AreXmlElementsEqual(ourElement, ancestorElement))
                    {
                        ours.RemoveChild(ourElement);                        
                    }
                    else
                    {
                        //todo log conflict
                    }
                }
            }

            return ours;
        }
    }
}
