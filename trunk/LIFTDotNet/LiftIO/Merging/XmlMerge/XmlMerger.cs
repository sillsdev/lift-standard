using System.Collections.Generic;
using System.Diagnostics;
using System.Xml;
using LiftIO.Merging.XmlMerge;

namespace LiftIO.Tests.Merging
{
    public class MergeResult
    {
        private XmlNode _mergedNode;
        private IList<IConflict> _conflicts;

        public MergeResult()
        {
            _conflicts = new List<IConflict>();
        }

        public XmlNode MergedNode
        {
            get
            {
                return _mergedNode;
            }
            internal set { _mergedNode = value; }
        }

        public IList<IConflict> Conflicts
        {
            get { return _conflicts; }
            set { _conflicts = value; }
        }
    }

    public class XmlMerger
    {
        public IMergeLogger _logger;
        public MergeStrategies _mergeStrategies;

        public XmlMerger()
        {
            _mergeStrategies = new MergeStrategies();
            
        }

        public MergeResult Merge(XmlNode ours, XmlNode theirs, XmlNode ancestor)
        {
            MergeResult result = new MergeResult();
            _logger = new MergeLogger(result.Conflicts);
            MergeInner(ref ours, theirs, ancestor);
            result.MergedNode = ours;
            return result;
        }

        public void MergeInner(ref XmlNode ours, XmlNode theirs, XmlNode ancestor)
        {
            MergeAttributes(ref ours, theirs, ancestor);
            MergeChildren(ref ours,theirs,ancestor);
        }

        public MergeResult Merge(string ours, string theirs, string ancestor)
        {
            XmlDocument doc = new XmlDocument();
            XmlNode ourNode = Utilities.GetDocumentNodeFromRawXml(ours, doc);
            XmlNode theirNode = Utilities.GetDocumentNodeFromRawXml(theirs, doc);
            XmlNode ancestorNode = Utilities.GetDocumentNodeFromRawXml(ancestor, doc);

            return Merge(ourNode, theirNode, ancestorNode);
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

        private void MergeAttributes(ref XmlNode ours, XmlNode theirs, XmlNode ancestor)
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
                        _logger.RegisterConflict(new RemovedVsEditedAttributeConflict(theirAttr.Name, null, theirAttr.Value, ancestorAttr.Value, _mergeStrategies));
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
                        _logger.RegisterConflict(new BothEdittedAttributeConflict(theirAttr.Name, ourAttr.Value, theirAttr.Value, null,  _mergeStrategies));
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
                else if (ourAttr.Value == theirAttr.Value)
                {
                    //both changed to same value
                    continue;
                }
                else if (ancestorAttr.Value == theirAttr.Value)
                {
                    //only we changed the value
                    continue;
                }
                else
                {
                    _logger.RegisterConflict(new BothEdittedAttributeConflict(theirAttr.Name, ourAttr.Value, theirAttr.Value, ancestorAttr.Value, _mergeStrategies));
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
                        _logger.RegisterConflict(new RemovedVsEditedAttributeConflict(ourAttr.Name, ourAttr.Value, null, ancestorAttr.Value, _mergeStrategies));
                    }
                }
            }
        }

        private void MergeTextNodes(ref XmlNode ours, XmlNode theirs, XmlNode ancestor)
        {
            if (ours.InnerText.Trim() == theirs.InnerText.Trim())
            {
                return; // we agree
            }
            if (string.IsNullOrEmpty(ours.InnerText.Trim()))
            {
                if (ancestor == null || ancestor.InnerText ==null || ancestor.InnerText.Trim()==string.Empty)
                {
                    ours.InnerText = theirs.InnerText; //we had it empty
                    return;
                }
                else  //we deleted it.
                {
                    if (ancestor.InnerText.Trim() == theirs.InnerText.Trim())
                    {
                        //and they didn't touch it. So leave it deleted    
                        return;
                    }
                    else
                    {
                        //they edited it. Keep our removal.
                        _logger.RegisterConflict(new RemovedVsEdittedTextConflict(ours, theirs, ancestor, _mergeStrategies));
                        return;
                    }
                }
            }
            else if ((ancestor == null) || (ours.InnerText != ancestor.InnerText)) 
            {
                //we're not empty, we edited it, and we don't equal theirs

                if (theirs.InnerText == null || string.IsNullOrEmpty(theirs.InnerText.Trim()))
                {
                    //we edited, they deleted it. Keep ours.
                    _logger.RegisterConflict(new RemovedVsEdittedTextConflict(ours, theirs, ancestor, _mergeStrategies));
                    return;
                }
                else
                {   //both edited it. Keep ours.
                    _logger.RegisterConflict(new BothEdittedTextConflict(ours, theirs, ancestor, _mergeStrategies));
                    return;
                }
            }
            else // we didn't edit it, they did
            {
                ours.InnerText = theirs.InnerText;
            }
        }

        private void MergeChildren(ref XmlNode ours, XmlNode theirs, XmlNode ancestor)
        {
            foreach (XmlNode theirChild in theirs.ChildNodes)
            {

                if(theirChild.NodeType != XmlNodeType.Element && (theirChild.NodeType != XmlNodeType.Text))
                {
                    if (theirChild.NodeType == XmlNodeType.Whitespace)
                    {
                        continue;
                    }
                    Debug.Fail("We don't know how to merge this type of child: "+theirChild.NodeType.ToString());
                    continue; //we don't know about merging other kinds of things
                }


                IFindNodeToMerge finder = _mergeStrategies.GetMergePartnerFinder(theirChild);
                XmlNode ourChild = finder.GetNodeToMerge(theirChild, ours);
                XmlNode ancestorChild = finder.GetNodeToMerge(theirChild, ancestor);


                if (theirChild.NodeType == XmlNodeType.Text)
                {
                    MergeTextNodes(ref ourChild, theirChild, ancestorChild);
                    continue;
                }

                if (ourChild == null)
                {
                    if (ancestorChild == null)
                    {
                        ours.AppendChild(Utilities.GetDocumentNodeFromRawXml(theirChild.OuterXml, ours.OwnerDocument));
                    }
                    else if (Utilities.AreXmlElementsEqual(ancestorChild, theirChild))
                    {
                        continue; // we deleted it, they didn't touch it
                    }
                    else //we deleted it, but at the same time, they changed it
                    {
                        _logger.RegisterConflict(new RemovedVsEditedElementConflict(theirChild.Name, null, theirChild, ancestorChild, _mergeStrategies));
                        continue;
                    }
                }
                else if ((ancestorChild!=null) && Utilities.AreXmlElementsEqual(ourChild, ancestorChild))
                {
                    if (Utilities.AreXmlElementsEqual(ourChild, theirChild))
                    {
                        //nothing to do
                        continue;
                    }
                    else //theirs is new
                    {
                        MergeInner(ref ourChild, theirChild, ancestorChild);
                    }
                }
                else
                {
                    MergeInner(ref ourChild, theirChild, ancestorChild);
                }
            }

            // deal with their deletions (elements and text)
            foreach (XmlNode ourChild in ours.ChildNodes)
            {
                if (ourChild.NodeType != XmlNodeType.Element && ourChild.NodeType != XmlNodeType.Text)
                {
                    continue;
                }
                IFindNodeToMerge finder = _mergeStrategies.GetMergePartnerFinder(ourChild);
                XmlNode ancestorChild = finder.GetNodeToMerge(ourChild, ancestor);
                XmlNode theirChild = finder.GetNodeToMerge(ourChild, theirs);

                if (theirChild == null && ancestorChild != null)
                {
                    if (Utilities.AreXmlElementsEqual(ourChild, ancestorChild))
                    {
                        ours.RemoveChild(ourChild);
                    }
                    else
                    {
                        if (ourChild.NodeType == XmlNodeType.Element)
                        {
                            _logger.RegisterConflict(
                                new RemovedVsEditedElementConflict(ourChild.Name, ourChild, null, ancestorChild, _mergeStrategies));
                        }
                        else
                        {
                            _logger.RegisterConflict(
                                new RemovedVsEdittedTextConflict(ourChild, null, ancestorChild, _mergeStrategies));
                        }
                    }
                }
            }
        }



    }

}
